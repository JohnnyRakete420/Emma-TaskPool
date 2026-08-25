using System.Windows;
using System.Windows.Controls;
using Emma.Shared;
using Emma.Shared.Dtos;

namespace Emma.TrayApp;

/// <summary>Formular zum Anlegen ODER Bearbeiten eines wiederkehrenden Plans (je nach Konstruktor).</summary>
public partial class PlanBearbeitenWindow : Window
{
    private static readonly DayOfWeek[] WochentagsReihenfolge =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    private readonly EmmaApiClient _api = new(App.Config);
    private readonly int? _bearbeiteId;
    private readonly List<TagEingabe> _tage;

    /// <summary>Neuen Plan anlegen.</summary>
    public PlanBearbeitenWindow() : this(null) { }

    /// <summary>Bestehenden Plan bearbeiten.</summary>
    public PlanBearbeitenWindow(WiederkehrenderPlanDto? vorhandenerPlan)
    {
        InitializeComponent();

        _bearbeiteId = vorhandenerPlan?.Id;
        TitelText.Text = vorhandenerPlan is null ? "Neuen Plan anlegen" : "Plan bearbeiten";
        LoeschenButton.Visibility = vorhandenerPlan is null ? Visibility.Collapsed : Visibility.Visible;

        _tage = WochentagsReihenfolge
            .Select(t => new TagEingabe { Wochentag = t, Anzeigename = PlanJsonHelper.DeutscherWochentag(t) })
            .ToList();

        if (vorhandenerPlan is not null)
        {
            foreach (var zeitpunkt in vorhandenerPlan.Zeitpunkte)
            {
                var tag = _tage.First(t => t.Wochentag == zeitpunkt.Wochentag);
                tag.IstAktiv = true;
                tag.Zeiten.Add(new ZeitWert { Wert = zeitpunkt.Uhrzeit.ToString("HH:mm") });
            }
        }

        TageItemsControl.ItemsSource = _tage;

        Loaded += async (_, _) => await LadeProzesseAsync(vorhandenerPlan?.ProzessId, vorhandenerPlan?.ParameterWerte);
    }

    private async Task LadeProzesseAsync(int? vorausgewaehlteProzessId, List<ParameterFeldWert>? vorhandeneWerte)
    {
        try
        {
            var prozesse = await _api.GetProzesseAsync();
            ProzessComboBox.ItemsSource = prozesse;

            var vorauswahl = vorausgewaehlteProzessId is { } id
                ? prozesse.FirstOrDefault(p => p.Id == id)
                : prozesse.FirstOrDefault();

            if (vorauswahl is not null)
            {
                ProzessComboBox.SelectedItem = vorauswahl;
                ZeigeParameterFelder(vorauswahl.ParameterFelder, vorhandeneWerte);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Prozesse konnten nicht geladen werden: {ex.Message}";
        }
    }

    private void ProzessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProzessComboBox.SelectedItem is ProzessDto prozess)
            ZeigeParameterFelder(prozess.ParameterFelder, null);
        else
            VerbergeParameterFelder();
    }

    private void ZeigeParameterFelder(List<ParameterFeldDefinition> felder, List<ParameterFeldWert>? vorhandeneWerte)
    {
        if (felder.Count == 0)
        {
            VerbergeParameterFelder();
            return;
        }

        ParameterItemsControl.ItemsSource = felder
            .Select(feld => ParameterEingabe.Neu(feld, vorhandeneWerte?.FirstOrDefault(w => w.Bezeichnung == feld.Bezeichnung)?.Wert))
            .ToList();
        ParameterPanel.Visibility = Visibility.Visible;
    }

    private void VerbergeParameterFelder()
    {
        ParameterPanel.Visibility = Visibility.Collapsed;
        ParameterItemsControl.ItemsSource = null;
    }

    private void TagCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.CheckBox { DataContext: TagEingabe tag })
            return;

        if (tag.IstAktiv && tag.Zeiten.Count == 0)
            tag.Zeiten.Add(new ZeitWert());
    }

    private void ZeitHinzufuegen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: TagEingabe tag })
            tag.Zeiten.Add(new ZeitWert());
    }

    private void ZeitEntfernen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: ZeitWert zeit })
            return;

        var tag = _tage.FirstOrDefault(t => t.Zeiten.Contains(zeit));
        tag?.Zeiten.Remove(zeit);
    }

    private async void SpeichernButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProzessComboBox.SelectedItem is not ProzessDto prozess)
        {
            StatusText.Text = "Bitte einen Prozess auswählen.";
            return;
        }

        var zeitpunkte = new List<PlanZeitpunkt>();
        foreach (var tag in _tage.Where(t => t.IstAktiv))
        {
            if (tag.Zeiten.Count == 0)
            {
                StatusText.Text = $"Bitte für \"{tag.Anzeigename}\" mindestens eine Uhrzeit angeben oder den Tag abwählen.";
                return;
            }

            foreach (var zeitWert in tag.Zeiten)
            {
                if (!TimeOnly.TryParse(zeitWert.Wert, out var uhrzeit))
                {
                    StatusText.Text = $"Uhrzeit \"{zeitWert.Wert}\" bei \"{tag.Anzeigename}\" ist ungültig (Format HH:mm, z.B. 20:00).";
                    return;
                }

                zeitpunkte.Add(new PlanZeitpunkt(tag.Wochentag, uhrzeit));
            }
        }

        if (zeitpunkte.Count == 0)
        {
            StatusText.Text = "Bitte mindestens einen Wochentag auswählen.";
            return;
        }

        var eingaben = (ParameterItemsControl.ItemsSource as List<ParameterEingabe>) ?? [];
        var fehlendeFelder = eingaben.Where(f => string.IsNullOrWhiteSpace(f.ErmittleWert())).Select(f => f.Bezeichnung).ToList();
        if (fehlendeFelder.Count > 0)
        {
            StatusText.Text = $"Bitte angeben: {string.Join(", ", fehlendeFelder)}.";
            return;
        }

        var parameterWerte = eingaben.Select(f => new ParameterFeldWert(f.Bezeichnung, f.ErmittleWert().Trim())).ToList();
        var request = new NeuerWiederkehrenderPlanRequest(prozess.Id, zeitpunkte, parameterWerte);

        try
        {
            if (_bearbeiteId is { } id)
                await _api.AktualisierePlanAsync(id, request);
            else
                await _api.ErstellePlanAsync(request);

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
            await _api.LoeschePlanAsync(id);
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
