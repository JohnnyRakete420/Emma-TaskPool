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
            ProzessListBox.ItemsSource = prozesse;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Prozesse konnten nicht geladen werden. Läuft der Emma.Service? ({ex.Message})";
        }
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
            StatusText.Text = $"\"{prozess.Name}\" wurde an EMMA übergeben.";

            ParameterItemsControl.ItemsSource = null;
            ParameterItemsControl.ItemsSource = prozess.ParameterFelder.Select(feld => ParameterEingabe.Neu(feld)).ToList();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Anlegen der Aufgabe: {ex.Message}";
        }
        finally
        {
            AufgabeAnlegenButton.IsEnabled = true;
        }
    }
}
