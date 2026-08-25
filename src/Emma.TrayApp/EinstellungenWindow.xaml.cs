using System.Reflection;
using System.Windows;

namespace Emma.TrayApp;

public partial class EinstellungenWindow : Window
{
    private LokaleEinstellungenDaten _daten = LokaleEinstellungen.Lade();

    public EinstellungenWindow()
    {
        InitializeComponent();

        AutostartCheckBox.IsChecked = App.IstAutostartAktiv();
        BenachrichtigungenCheckBox.IsChecked = _daten.BenachrichtigungenAktiv;

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

    private void SchliessenButton_Click(object sender, RoutedEventArgs e) => Close();
}
