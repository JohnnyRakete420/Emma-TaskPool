using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Emma.Shared;
using Emma.Shared.Dtos;
using Emma.Shared.Models;

namespace Emma.TrayApp;

public partial class VerlaufWindow : Window
{
    private readonly EmmaApiClient _api = new(App.Config);
    private List<AufgabeDto> _alleAufgaben = [];

    public VerlaufWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LadeAsync();
    }

    private async Task LadeAsync()
    {
        try
        {
            _alleAufgaben = await _api.GetAufgabenHistorieAsync();

            var prozessNamen = _alleAufgaben.Select(a => a.ProzessName).Distinct().OrderBy(n => n);
            ProzessFilterComboBox.ItemsSource = new[] { "Alle" }.Concat(prozessNamen).ToList();
            ProzessFilterComboBox.SelectedIndex = 0;

            var statusWerte = Enum.GetValues<AufgabeStatus>().Select(s => s.ToString());
            StatusFilterComboBox.ItemsSource = new[] { "Alle" }.Concat(statusWerte).ToList();
            StatusFilterComboBox.SelectedIndex = 0;

            AktualisiereStatistik();
            AktualisiereListe();
            StatusText.Text = $"{_alleAufgaben.Count} Aufgabe(n) insgesamt geladen.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Laden: {ex.Message}";
        }
    }

    private void AktualisiereStatistik()
    {
        var prozessFilter = ProzessFilterComboBox.SelectedItem as string;
        IEnumerable<AufgabeDto> basis = (prozessFilter is null || prozessFilter == "Alle")
            ? _alleAufgaben
            : _alleAufgaben.Where(a => a.ProzessName == prozessFilter);

        var basisListe = basis.ToList();
        var erledigt = basisListe.Count(a => a.Status == AufgabeStatus.Erledigt);
        var fehlgeschlagen = basisListe.Count(a => a.Status == AufgabeStatus.Fehlgeschlagen);
        var abgeschlossen = erledigt + fehlgeschlagen;

        GesamtText.Text = basisListe.Count.ToString();
        ErledigtText.Text = erledigt.ToString();
        FehlgeschlagenText.Text = fehlgeschlagen.ToString();
        ErfolgsquoteText.Text = abgeschlossen == 0 ? "–" : $"{(double)erledigt / abgeschlossen:P0}";
    }

    private void AktualisiereListe()
    {
        if (!IsLoaded)
            return;

        IEnumerable<AufgabeDto> gefiltert = _alleAufgaben;

        if (ProzessFilterComboBox.SelectedItem is string prozessFilter && prozessFilter != "Alle")
            gefiltert = gefiltert.Where(a => a.ProzessName == prozessFilter);

        if (StatusFilterComboBox.SelectedItem is string statusFilter && statusFilter != "Alle"
            && Enum.TryParse<AufgabeStatus>(statusFilter, out var status))
            gefiltert = gefiltert.Where(a => a.Status == status);

        if (VonDatePicker.SelectedDate is { } von)
            gefiltert = gefiltert.Where(a => a.ErstelltAm.Date >= von.Date);

        if (BisDatePicker.SelectedDate is { } bis)
            gefiltert = gefiltert.Where(a => a.ErstelltAm.Date <= bis.Date);

        if (!string.IsNullOrWhiteSpace(SucheTextBox.Text))
            gefiltert = gefiltert.Where(a => a.ErstelltVon.Contains(SucheTextBox.Text, StringComparison.OrdinalIgnoreCase));

        HistorieGrid.ItemsSource = gefiltert
            .OrderByDescending(a => a.ErstelltAm)
            .Select(a => new VerlaufZeile(a.Id, a.ProzessName, a.Status, ParameterJsonHelper.FormatiereWerte(a.ParameterWerte), a.ErstelltAm))
            .ToList();
    }

    private void ProzessFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AktualisiereStatistik();
        AktualisiereListe();
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) => AktualisiereListe();
    private void Filter_DateChanged(object sender, EventArgs e) => AktualisiereListe();
    private void Filter_TextChanged(object sender, TextChangedEventArgs e) => AktualisiereListe();

    private void HistorieGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HistorieGrid.SelectedItem is not VerlaufZeile zeile)
            return;

        var aufgabe = _alleAufgaben.FirstOrDefault(a => a.Id == zeile.Id);
        if (aufgabe is null)
            return;

        new AufgabeVerlaufDetailsWindow(aufgabe) { Owner = this }.ShowDialog();
    }

    private async void AktualisierenButton_Click(object sender, RoutedEventArgs e) => await LadeAsync();
}

internal record VerlaufZeile(int Id, string ProzessName, AufgabeStatus Status, string ParameterText, DateTime ErstelltAm);
