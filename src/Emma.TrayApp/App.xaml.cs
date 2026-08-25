using System.Windows;
using System.Windows.Threading;
using Emma.Shared;
using Emma.Shared.Models;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Emma.TrayApp;

/// <summary>
/// Startet ohne sichtbares Fenster im System Tray ("ausgeblendete Symbole").
/// Von dort aus öffnet der Benutzer die Prozessauswahl, verwaltet wiederkehrende
/// Pläne und sieht den Verlauf. Im Hintergrund wird zusätzlich per Polling geprüft,
/// ob EMMA eine eigene Aufgabe erledigt oder als fehlgeschlagen markiert hat, und
/// dazu eine Sprechblasen-Benachrichtigung angezeigt.
/// </summary>
public partial class App : System.Windows.Application
{
    private const string AutostartRegistryPfad = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AutostartName = "EmmaTrayApp";

    public static readonly EmmaClientConfig Config = EmmaConfig.Lade();

    private readonly EmmaApiClient _api = new(Config);
    private readonly HashSet<int> _bekannteAbgeschlossenenIds = [];

    private Forms.NotifyIcon? _notifyIcon;
    private ProzessAuswahlWindow? _prozessFenster;
    private WiederkehrendePlaeneWindow? _plaeneFenster;
    private VerlaufWindow? _verlaufFenster;
    private EinstellungenWindow? _einstellungenFenster;
    private ProzesseVerwaltenWindow? _prozesseVerwaltenFenster;
    private DispatcherTimer? _benachrichtigungsTimer;

    private System.Drawing.Icon? _basisIcon;
    private bool? _letzterVerbindungsStatus;
    private IntPtr _aktuellesStatusIconHandle;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    private const int DwmwaUseImmersiveDarkMode = 20;

    // Windows 11 zeichnet um jedes Fenster einen dünnen Akzentrahmen (separat von der
    // Titelleiste) - DWMWA_USE_IMMERSIVE_DARK_MODE allein färbt den nicht mit, deshalb
    // zusätzlich DWMWA_BORDER_COLOR setzen. Nur ab Windows 11 22H2 unterstützt; auf älteren
    // Systemen schlägt der Aufruf einfach folgenlos fehl (kein Absturz).
    private const int DwmwaBorderColor = 34;

    /// <summary>Setzt Titelleiste UND Rahmen jedes Fensters (Windows 10 1809+/11) auf dunkel,
    /// damit kein heller Fremdkörper um den dunklen Fensterinhalt übrig bleibt.</summary>
    private static void FaerbeTitelleisteDunkel(Window fenster)
    {
        var handle = new System.Windows.Interop.WindowInteropHelper(fenster).Handle;
        if (handle == IntPtr.Zero)
            return;

        var dunkelModus = 1;
        DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref dunkelModus, sizeof(int));

        // COLORREF (0x00BBGGRR) für die dunkle Seitenhintergrundfarbe #1B2416 aus ColorsDark.xaml.
        var rahmenfarbe = 0x00162419;
        DwmSetWindowAttribute(handle, DwmwaBorderColor, ref rahmenfarbe, sizeof(int));
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Farbpalette + Styles zur Laufzeit mergen (statt statisch in App.xaml), damit das
        // Dunkle Design (Einstellungen-Fenster) ausgewählt werden kann, bevor irgendein
        // Fenster erzeugt wird. Wirkt erst nach einem Neustart, kein Live-Umschalten.
        var dunkel = LokaleEinstellungen.Lade().DarkMode;
        var farbdatei = dunkel ? "ColorsDark.xaml" : "ColorsLight.xaml";
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(farbdatei, UriKind.Relative) });
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("Styles.xaml", UriKind.Relative) });

        // Bei Dunklem Design auch die native Titelleiste jedes Fensters einfärben (sonst bliebe
        // sie hell, egal was im Fenster steht) - für alle Fenster zentral statt pro Fenster.
        if (dunkel)
        {
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent,
                new RoutedEventHandler((sender, _) => FaerbeTitelleisteDunkel((Window)sender)));
        }

        // Sicherheitsnetz: ein unerwarteter Fehler soll eine Meldung zeigen statt die
        // Anwendung stillschweigend abstürzen zu lassen.
        DispatcherUnhandledException += (_, args) =>
        {
            Forms.MessageBox.Show(
                $"Unerwarteter Fehler: {args.Exception.Message}",
                "EMMA Aufgabenpool", Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
            args.Handled = true;
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Prozess auswählen...", null, (_, _) => ZeigeProzessFenster());
        menu.Items.Add("Wiederkehrende Pläne verwalten...", null, (_, _) => ZeigePlaeneFenster());
        menu.Items.Add("Prozesse verwalten...", null, (_, _) => ZeigeProzesseVerwaltenFenster());
        menu.Items.Add("Verlauf & Übersicht...", null, (_, _) => ZeigeVerlaufFenster());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Einstellungen...", null, (_, _) => ZeigeEinstellungenFenster());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => BeendenAnwendung());

        // MainModule.FileName statt Assembly.Location: nur die .exe trägt das per
        // ApplicationIcon eingebettete Icon, nicht die .dll.
        var exePfad = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
        _basisIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePfad) ?? System.Drawing.SystemIcons.Application;

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _basisIcon,
            Visible = true,
            Text = "EMMA Aufgabenpool",
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => ZeigeProzessFenster();

        _ = StarteBenachrichtigungenAsync();
    }

    private void ZeigeProzessFenster()
    {
        if (_prozessFenster is null || !_prozessFenster.IsLoaded)
            _prozessFenster = new ProzessAuswahlWindow();

        _prozessFenster.Show();
        _prozessFenster.Activate();
    }

    private void ZeigePlaeneFenster()
    {
        if (_plaeneFenster is null || !_plaeneFenster.IsLoaded)
            _plaeneFenster = new WiederkehrendePlaeneWindow();

        _plaeneFenster.Show();
        _plaeneFenster.Activate();
    }

    private void ZeigeVerlaufFenster()
    {
        if (_verlaufFenster is null || !_verlaufFenster.IsLoaded)
            _verlaufFenster = new VerlaufWindow();

        _verlaufFenster.Show();
        _verlaufFenster.Activate();
    }

    private void ZeigeEinstellungenFenster()
    {
        if (_einstellungenFenster is null || !_einstellungenFenster.IsLoaded)
            _einstellungenFenster = new EinstellungenWindow();

        _einstellungenFenster.Show();
        _einstellungenFenster.Activate();
    }

    private void ZeigeProzesseVerwaltenFenster()
    {
        if (_prozesseVerwaltenFenster is null || !_prozesseVerwaltenFenster.IsLoaded)
            _prozesseVerwaltenFenster = new ProzesseVerwaltenWindow();

        _prozesseVerwaltenFenster.Show();
        _prozesseVerwaltenFenster.Activate();
    }

    internal static bool IstAutostartAktiv()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutostartRegistryPfad, writable: false);
        return key?.GetValue(AutostartName) is not null;
    }

    internal static void SetzeAutostart(bool aktiv)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutostartRegistryPfad, writable: true);
        if (key is null)
            return;

        if (aktiv)
        {
            var exePfad = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
            key.SetValue(AutostartName, $"\"{exePfad}\"");
        }
        else
        {
            key.DeleteValue(AutostartName, throwOnMissingValue: false);
        }
    }

    private async Task StarteBenachrichtigungenAsync()
    {
        // Bereits abgeschlossene Aufgaben beim Start als "bekannt" markieren, damit
        // beim ersten Poll nicht sofort für alte Aufgaben benachrichtigt wird.
        try
        {
            var erledigte = await _api.GetAufgabenAsync(AufgabeStatus.Erledigt);
            var fehlgeschlagene = await _api.GetAufgabenAsync(AufgabeStatus.Fehlgeschlagen);
            foreach (var aufgabe in erledigte.Concat(fehlgeschlagene))
                _bekannteAbgeschlossenenIds.Add(aufgabe.Id);
        }
        catch
        {
            // Service beim Start evtl. noch nicht erreichbar - Timer versucht es weiter unten regelmäßig.
        }

        _benachrichtigungsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _benachrichtigungsTimer.Tick += async (_, _) => await PruefeAbgeschlosseneAufgabenAsync();
        _benachrichtigungsTimer.Start();
    }

    private async Task PruefeAbgeschlosseneAufgabenAsync()
    {
        try
        {
            var erledigte = await _api.GetAufgabenAsync(AufgabeStatus.Erledigt);
            var fehlgeschlagene = await _api.GetAufgabenAsync(AufgabeStatus.Fehlgeschlagen);
            var benachrichtigungenAktiv = LokaleEinstellungen.Lade().BenachrichtigungenAktiv;
            AktualisiereStatusIcon(verbunden: true);

            foreach (var aufgabe in erledigte.Where(a => a.ErstelltVon == Environment.UserName))
            {
                if (_bekannteAbgeschlossenenIds.Add(aufgabe.Id) && benachrichtigungenAktiv)
                {
                    _notifyIcon!.BalloonTipIcon = Forms.ToolTipIcon.Info;
                    _notifyIcon.BalloonTipTitle = "EMMA hat eine Aufgabe erledigt";
                    _notifyIcon.BalloonTipText = aufgabe.ProzessName;
                    _notifyIcon.ShowBalloonTip(5000);
                }
            }

            foreach (var aufgabe in fehlgeschlagene.Where(a => a.ErstelltVon == Environment.UserName))
            {
                if (_bekannteAbgeschlossenenIds.Add(aufgabe.Id) && benachrichtigungenAktiv)
                {
                    _notifyIcon!.BalloonTipIcon = Forms.ToolTipIcon.Warning;
                    _notifyIcon.BalloonTipTitle = "EMMA konnte eine Aufgabe nicht abschließen";
                    _notifyIcon.BalloonTipText = $"{aufgabe.ProzessName}: {aufgabe.Fehlermeldung}";
                    _notifyIcon.ShowBalloonTip(8000);
                }
            }
        }
        catch
        {
            // Service nicht erreichbar - nächster Tick versucht es erneut.
            AktualisiereStatusIcon(verbunden: false);
        }
    }

    /// <summary>
    /// Überlagert das Tray-Symbol mit einem grünen/roten Punkt je nach Erreichbarkeit des
    /// Service, damit ein Ausfall sofort auffällt statt erst beim Öffnen eines Fensters.
    /// Ändert das Icon nur bei einem tatsächlichen Statuswechsel (nicht bei jedem Poll).
    /// </summary>
    private void AktualisiereStatusIcon(bool verbunden)
    {
        if (_letzterVerbindungsStatus == verbunden || _basisIcon is null || _notifyIcon is null)
            return;
        _letzterVerbindungsStatus = verbunden;

        using var bmp = _basisIcon.ToBitmap();
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var farbe = verbunden
                ? System.Drawing.Color.FromArgb(147, 192, 3)
                : System.Drawing.Color.FromArgb(217, 83, 79);
            var durchmesser = bmp.Width / 2.4f;
            var rect = new System.Drawing.RectangleF(bmp.Width - durchmesser, bmp.Height - durchmesser, durchmesser, durchmesser);
            using var brush = new System.Drawing.SolidBrush(farbe);
            using var stift = new System.Drawing.Pen(System.Drawing.Color.White, 1.5f);
            g.FillEllipse(brush, rect);
            g.DrawEllipse(stift, rect);
        }

        var neuerHandle = bmp.GetHicon();
        var alterHandle = _aktuellesStatusIconHandle;

        _notifyIcon.Icon = System.Drawing.Icon.FromHandle(neuerHandle);
        _aktuellesStatusIconHandle = neuerHandle;
        _notifyIcon.Text = verbunden ? "EMMA Aufgabenpool" : "EMMA Aufgabenpool (Service nicht erreichbar)";

        if (alterHandle != IntPtr.Zero)
            DestroyIcon(alterHandle);
    }

    private void BeendenAnwendung()
    {
        _benachrichtigungsTimer?.Stop();
        _notifyIcon!.Visible = false;
        _notifyIcon.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        if (_aktuellesStatusIconHandle != IntPtr.Zero)
            DestroyIcon(_aktuellesStatusIconHandle);
        base.OnExit(e);
    }
}
