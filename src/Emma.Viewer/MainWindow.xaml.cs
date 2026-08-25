using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Emma.Shared;
using Emma.Shared.Dtos;
using Emma.Shared.Models;

namespace Emma.Viewer;

/// <summary>
/// Fenster, das EMMA per Screen-Scan beobachtet: sie liest hier neue Aufgaben, führt den
/// passenden Ablaufplan aus und klickt danach selbst auf "Erledigt" oder - falls der Prozess
/// nicht durchführbar war - auf "Fehlgeschlagen". Bei Aufgaben mit Formularwerten öffnet
/// "Bearbeiten" zusätzlich ein eigenes Detail-Fenster, damit sie die Werte klar getrennt
/// (statt als eine zusammengequetschte Textzeile) lesen kann.
/// </summary>
public partial class MainWindow : Window
{
    private readonly EmmaApiClient _api = new(App.Config);
    private readonly DispatcherTimer _timer;
    private List<AufgabeDto> _offeneAufgaben = [];

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += async (_, _) => await LadeAufgabenAsync();
        _timer.Start();

        Loaded += async (_, _) => await LadeAufgabenAsync();
    }

    private async Task LadeAufgabenAsync()
    {
        try
        {
            var aufgaben = await _api.GetAufgabenAsync();
            _offeneAufgaben = aufgaben
                .Where(a => a.Status is AufgabeStatus.Neu or AufgabeStatus.InBearbeitung)
                .ToList();

            AufgabenListe.ItemsSource = _offeneAufgaben.Select(a => new
            {
                a.Id,
                a.ProzessName,
                a.ParameterWerte,
                ErstelltInfo = $"Erstellt von {a.ErstelltVon} · {a.ErstelltAm:g}",
                StatusText = a.Status == AufgabeStatus.InBearbeitung ? "Wird bearbeitet" : "Neu",
                HatParameter = a.ParameterWerte.Count > 0,
                ZeigeBearbeiten = a.ParameterWerte.Count > 0 && a.Status == AufgabeStatus.Neu
            }).ToList();

            StatusText.Text = $"Zuletzt aktualisiert: {DateTime.Now:HH:mm:ss} – {_offeneAufgaben.Count} offene Aufgabe(n)";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Verbindung zum Emma.Service fehlgeschlagen: {ex.Message}";
        }
    }

    private async void BearbeitenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int aufgabeId })
            return;

        var aufgabe = _offeneAufgaben.FirstOrDefault(a => a.Id == aufgabeId);
        if (aufgabe is null)
            return;

        try
        {
            await _api.MarkiereInBearbeitungAsync(aufgabeId);
            await LadeAufgabenAsync();

            var detailsFenster = new AufgabeDetailsWindow(aufgabe.ProzessName, aufgabe.ParameterWerte)
            {
                Owner = this
            };
            detailsFenster.ShowDialog();

            WindowState = WindowState.Minimized;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Öffnen der Details: {ex.Message}";
        }
    }

    private async void ErledigtButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int aufgabeId })
            return;

        try
        {
            await _api.MarkiereErledigtAsync(aufgabeId);
            await LadeAufgabenAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Markieren als erledigt: {ex.Message}";
        }
    }

    private async void FehlgeschlagenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int aufgabeId })
            return;

        try
        {
            await _api.MarkiereFehlgeschlagenAsync(aufgabeId);
            await LadeAufgabenAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Markieren als fehlgeschlagen: {ex.Message}";
        }
    }
}
