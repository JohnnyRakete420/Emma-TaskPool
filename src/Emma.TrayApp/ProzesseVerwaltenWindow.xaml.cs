using System.Windows;
using System.Windows.Controls;
using Emma.Shared;
using Emma.Shared.Dtos;

namespace Emma.TrayApp;

public partial class ProzesseVerwaltenWindow : Window
{
    private readonly EmmaApiClient _api = new(App.Config);
    private List<ProzessDto> _prozesse = [];

    public ProzesseVerwaltenWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LadeAsync();
    }

    private async Task LadeAsync()
    {
        try
        {
            _prozesse = await _api.GetProzesseAsync();
            ProzesseGrid.ItemsSource = _prozesse
                .Select(p => new ProzessZeile(p.Id, p.Name, p.Beschreibung ?? "", p.ParameterFelder.Count))
                .ToList();
            StatusText.Text = $"{_prozesse.Count} Prozess(e) geladen.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Laden: {ex.Message}";
        }
    }

    private async void NeuButton_Click(object sender, RoutedEventArgs e)
    {
        var fenster = new ProzessBearbeitenWindow { Owner = this };
        if (fenster.ShowDialog() == true)
            await LadeAsync();
    }

    private async void BearbeitenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: int prozessId })
            return;

        var prozess = _prozesse.FirstOrDefault(p => p.Id == prozessId);
        if (prozess is null)
            return;

        var fenster = new ProzessBearbeitenWindow(prozess) { Owner = this };
        if (fenster.ShowDialog() == true)
            await LadeAsync();
    }
}

internal record ProzessZeile(int Id, string Name, string Beschreibung, int FelderAnzahl)
{
    public string FelderText => FelderAnzahl == 0 ? "keine" : $"{FelderAnzahl} Feld(er)";
}
