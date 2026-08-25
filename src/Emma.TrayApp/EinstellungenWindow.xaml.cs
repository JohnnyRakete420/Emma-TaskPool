using System.Reflection;
using System.Windows;
using Emma.Shared;

namespace Emma.TrayApp;

public partial class EinstellungenWindow : Window
{
    private LokaleEinstellungenDaten _daten = LokaleEinstellungen.Lade();

    public EinstellungenWindow()
    {
        InitializeComponent();

        AutostartCheckBox.IsChecked = App.IstAutostartAktiv();
        BenachrichtigungenCheckBox.IsChecked = _daten.BenachrichtigungenAktiv;
        DarkModeCheckBox.IsChecked = _daten.DarkMode;

        var config = EmmaConfig.Lade();
        ServerUrlTextBox.Text = config.ServiceBaseUrl;
        ApiKeyTextBox.Text = config.ApiKey;
        ZertifikatTextBox.Text = config.ZertifikatThumbprint ?? "";

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "Version unbekannt" : $"Version {version.ToString(3)}";
    }

    private void AutostartCheckBox_Click(object sender, RoutedEventArgs e) =>
        App.SetzeAutostart(AutostartCheckBox.IsChecked == true);

    private void BenachrichtigungenCheckBox_Click(object sender, RoutedEventArgs e)
    {
        _daten = _daten with { BenachrichtigungenAktiv = BenachrichtigungenCheckBox.IsChecked == true };
        LokaleEinstellungen.Speichere(_daten);
    }

    private void DarkModeCheckBox_Click(object sender, RoutedEventArgs e)
    {
        _daten = _daten with { DarkMode = DarkModeCheckBox.IsChecked == true };
        LokaleEinstellungen.Speichere(_daten);
    }

    private void VerbindungSpeichernButton_Click(object sender, RoutedEventArgs e)
    {
        var config = new EmmaClientConfig(
            ServerUrlTextBox.Text.Trim(),
            ApiKeyTextBox.Text.Trim(),
            string.IsNullOrWhiteSpace(ZertifikatTextBox.Text) ? null : ZertifikatTextBox.Text.Trim());
        EmmaConfig.Speichere(config);
        VerbindungStatusText.Text = "Gespeichert. Wirkt sich erst nach einem Neustart der Anwendung aus.";
        VerbindungStatusText.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
    }

    private async void VerbindungTestenButton_Click(object sender, RoutedEventArgs e)
    {
        VerbindungTestenButton.IsEnabled = false;
        VerbindungStatusText.Text = "Teste Verbindung...";
        VerbindungStatusText.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");

        try
        {
            var testConfig = new EmmaClientConfig(
                ServerUrlTextBox.Text.Trim(),
                ApiKeyTextBox.Text.Trim(),
                string.IsNullOrWhiteSpace(ZertifikatTextBox.Text) ? null : ZertifikatTextBox.Text.Trim());
            var testApi = new EmmaApiClient(testConfig);
            var prozesse = await testApi.GetProzesseAsync();

            VerbindungStatusText.Text = $"Verbindung erfolgreich. {prozesse.Count} Prozess(e) gefunden.";
            VerbindungStatusText.Foreground = (System.Windows.Media.Brush)FindResource("GreenBrush");
        }
        catch (Exception ex)
        {
            VerbindungStatusText.Text = $"Verbindung fehlgeschlagen: {ex.Message}";
            VerbindungStatusText.Foreground = (System.Windows.Media.Brush)FindResource("FehlerBrush");
        }
        finally
        {
            VerbindungTestenButton.IsEnabled = true;
        }
    }

    private void SchliessenButton_Click(object sender, RoutedEventArgs e) => Close();
}
