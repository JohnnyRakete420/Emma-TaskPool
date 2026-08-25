using System.Windows;
using Emma.Shared;
using Emma.Shared.Dtos;

namespace Emma.TrayApp;

public partial class ProzessAuswahlWindow : Window
{
    private readonly EmmaApiClient _api = new(App.Config);

    public ProzessAuswahlWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LadeProzesseAsync();
    }

    private async Task LadeProzesseAsync()
    {
        try
        {
            var prozesse = await _api.GetProzesseAsync();
            ProzessListBox.ItemsSource = SortiereNachZuletztVerwendet(prozesse);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Prozesse konnten nicht geladen werden. Läuft der Emma.Service? ({ex.Message})";
        }
    }

    /// <summary>Zuletzt verwendete Prozesse zuerst, damit häufig genutzte nicht immer wieder
    /// gesucht werden müssen. Nie verwendete Prozesse bleiben in der ursprünglichen Reihenfolge.</summary>
    private static List<ProzessDto> SortiereNachZuletztVerwendet(List<ProzessDto> prozesse)
    {
        var zuletztVerwendet = LokaleEinstellungen.Lade().ZuletztVerwendeteProzesse ?? [];
        return prozesse
            .Select((p, index) => (Prozess: p, Index: index))
            .OrderByDescending(x => zuletztVerwendet.TryGetValue(x.Prozess.Name, out var zeit) ? zeit : (DateTime?)null)
            .ThenBy(x => x.Index)
            .Select(x => x.Prozess)
            .ToList();
    }

    private static void MerkeAlsZuletztVerwendet(string prozessName)
    {
        var daten = LokaleEinstellungen.Lade();
        var zuletztVerwendet = new Dictionary<string, DateTime>(daten.ZuletztVerwendeteProzesse ?? []) { [prozessName] = DateTime.Now };
        LokaleEinstellungen.Speichere(daten with { ZuletztVerwendeteProzesse = zuletztVerwendet });
    }

    private void ProzessListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProzessListBox.SelectedItem is ProzessDto prozess && prozess.ParameterFelder.Count > 0)
        {
            ParameterItemsControl.ItemsSource = prozess.ParameterFelder
                .Select(feld => ParameterEingabe.Neu(feld))
                .ToList();
            ParameterScroll.Visibility = Visibility.Visible;
        }
        else
        {
            ParameterScroll.Visibility = Visibility.Collapsed;
            ParameterItemsControl.ItemsSource = null;
        }
    }

    private async void AufgabeAnlegenButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProzessListBox.SelectedItem is not ProzessDto prozess)
        {
            StatusText.Text = "Bitte zuerst einen Prozess auswählen.";
            return;
        }

        var eingaben = (ParameterItemsControl.ItemsSource as List<ParameterEingabe>) ?? [];
        var fehlendeFelder = eingaben.Where(f => string.IsNullOrWhiteSpace(f.ErmittleWert())).Select(f => f.Bezeichnung).ToList();
        if (fehlendeFelder.Count > 0)
        {
            StatusText.Text = $"Bitte angeben: {string.Join(", ", fehlendeFelder)}.";
            return;
        }

        AufgabeAnlegenButton.IsEnabled = false;
        try
        {
            var parameterWerte = eingaben
                .Select(f => new ParameterFeldWert(f.Bezeichnung, f.ErmittleWert().Trim()))
                .ToList();

            await _api.ErstelleAufgabeAsync(new NeueAufgabeRequest(prozess.Id, Environment.UserName, parameterWerte));
            MerkeAlsZuletztVerwendet(prozess.Name);

            ParameterItemsControl.ItemsSource = null;
            ParameterItemsControl.ItemsSource = prozess.ParameterFelder.Select(feld => ParameterEingabe.Neu(feld)).ToList();

            new AufgabeUebergebenWindow(erfolgreich: true, prozess.Name) { Owner = this }.ShowDialog();
        }
        catch (Exception ex)
        {
            new AufgabeUebergebenWindow(erfolgreich: false, prozess.Name, ex.Message) { Owner = this }.ShowDialog();
        }
        finally
        {
            AufgabeAnlegenButton.IsEnabled = true;
        }
    }
}
