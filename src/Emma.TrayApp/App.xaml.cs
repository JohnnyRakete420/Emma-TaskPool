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
    private DispatcherTimer? _benachrichtigungsTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

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
        menu.Items.Add("Verlauf & Übersicht...", null, (_, _) => ZeigeVerlaufFenster());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Einstellungen...", null, (_, _) => ZeigeEinstellungenFenster());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => BeendenAnwendung());

        // MainModule.FileName statt Assembly.Location: nur die .exe trägt das per
        // ApplicationIcon eingebettete Icon, nicht die .dll.
        var exePfad = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
        var eigenesIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePfad);

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = eigenesIcon ?? System.Drawing.SystemIcons.Application,
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
            // Service kurzzeitig nicht erreichbar - nächster Tick versucht es erneut.
        }
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
        base.OnExit(e);
    }
}
