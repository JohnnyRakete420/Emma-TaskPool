using System.Collections.ObjectModel;
using System.Windows;
using Emma.Shared;
using Emma.Shared.Dtos;

namespace Emma.TrayApp;

/// <summary>Formular zum Anlegen ODER Bearbeiten eines Prozesses (je nach Konstruktor).</summary>
public partial class ProzessBearbeitenWindow : Window
{
    private readonly EmmaApiClient _api = new(App.Config);
    private readonly int? _bearbeiteId;
    private readonly ObservableCollection<FeldDefinitionEingabe> _felder = [];

    /// <summary>Neuen Prozess anlegen.</summary>
    public ProzessBearbeitenWindow() : this(null) { }

    /// <summary>Bestehenden Prozess bearbeiten.</summary>
    public ProzessBearbeitenWindow(ProzessDto? vorhandenerProzess)
    {
        InitializeComponent();

        _bearbeiteId = vorhandenerProzess?.Id;
        TitelText.Text = vorhandenerProzess is null ? "Neuen Prozess anlegen" : "Prozess bearbeiten";
        LoeschenButton.Visibility = vorhandenerProzess is null ? Visibility.Collapsed : Visibility.Visible;

        NameTextBox.Text = vorhandenerProzess?.Name ?? "";
        BeschreibungTextBox.Text = vorhandenerProzess?.Beschreibung ?? "";

        if (vorhandenerProzess is not null)
            foreach (var feld in vorhandenerProzess.ParameterFelder)
                _felder.Add(FeldDefinitionEingabe.Von(feld));

        FelderItemsControl.ItemsSource = _felder;
    }

    private void FeldHinzufuegen_Click(object sender, RoutedEventArgs e) =>
        _felder.Add(new FeldDefinitionEingabe());

    private void FeldEntfernen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: FeldDefinitionEingabe feld })
            _felder.Remove(feld);
    }

    private async void SpeichernButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            StatusText.Text = "Bitte einen Namen angeben.";
            return;
        }

        var leeresFeld = _felder.Any(f => string.IsNullOrWhiteSpace(f.Bezeichnung));
        if (leeresFeld)
        {
            StatusText.Text = "Jedes Formularfeld benötigt eine Bezeichnung.";
            return;
        }

        var fehlendeOptionen = _felder.FirstOrDefault(f =>
            f.Typ != ParameterFeldTyp.Text
            && f.OptionenText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length == 0);
        if (fehlendeOptionen is not null)
        {
            StatusText.Text = $"Feld \"{fehlendeOptionen.Bezeichnung}\" benötigt mindestens eine Option (durch Komma getrennt).";
            return;
        }

        var request = new NeuerProzessRequest(
            NameTextBox.Text.Trim(),
            BeschreibungTextBox.Text,
            _felder.Select(f => f.ZuDefinition()).ToList());

        try
        {
            if (_bearbeiteId is { } id)
                await _api.AktualisiereProzessAsync(id, request);
            else
                await _api.ErstelleProzessAsync(request);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Speichern: {ex.Message}";
        }
    }

    private async void LoeschenButton_Click(object sender, RoutedEventArgs e)
    {
        if (_bearbeiteId is not { } id)
            return;

        try
        {
            await _api.LoescheProzessAsync(id);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Löschen: {ex.Message}";
        }
    }

    private void AbbrechenButton_Click(object sender, RoutedEventArgs e) => Close();
}
