using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Emma.Shared;
using Emma.Shared.Dtos;

namespace Emma.TrayApp;

public partial class WiederkehrendePlaeneWindow : Window
{
    private readonly EmmaApiClient _api = new(App.Config);
    private List<WiederkehrenderPlanDto> _plaene = [];

    public WiederkehrendePlaeneWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LadeAsync();
    }

    private async Task LadeAsync()
    {
        try
        {
            _plaene = await _api.GetWiederkehrendePlaeneAsync();
            PlaeneGrid.ItemsSource = _plaene.Select(p => new PlanZeile(
                p.Id, p.ProzessName,
                PlanJsonHelper.FormatiereZeitpunkte(p.Zeitpunkte),
                ParameterJsonHelper.FormatiereWerte(p.ParameterWerte))).ToList();
            StatusText.Text = $"{_plaene.Count} Plan/Pläne geladen.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Laden: {ex.Message}";
        }
    }

    private async void NeuButton_Click(object sender, RoutedEventArgs e)
    {
        var fenster = new PlanBearbeitenWindow { Owner = this };
        if (fenster.ShowDialog() == true)
            await LadeAsync();
    }

    private async void BearbeitenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: int planId })
            return;

        var plan = _plaene.FirstOrDefault(p => p.Id == planId);
        if (plan is null)
            return;

        var fenster = new PlanBearbeitenWindow(plan) { Owner = this };
        if (fenster.ShowDialog() == true)
            await LadeAsync();
    }

    private void PlaeneGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PlaeneGrid.SelectedItem is not PlanZeile zeile)
            return;

        var plan = _plaene.FirstOrDefault(p => p.Id == zeile.Id);
        if (plan is null)
            return;

        new PlanDetailsWindow(plan) { Owner = this }.ShowDialog();
    }
}

internal record PlanZeile(int Id, string ProzessName, string ZeitplanText, string ParameterText);
