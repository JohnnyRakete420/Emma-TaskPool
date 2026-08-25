using System.Windows;
using Emma.Shared.Dtos;
using Emma.Shared.Models;

namespace Emma.TrayApp;

/// <summary>Read-only Detailansicht für eine Zeile im Verlauf - per Doppelklick geöffnet.</summary>
public partial class AufgabeVerlaufDetailsWindow : Window
{
    public AufgabeVerlaufDetailsWindow(AufgabeDto aufgabe)
    {
        InitializeComponent();

        ProzessNameText.Text = aufgabe.ProzessName;
        StatusText.Text = aufgabe.Status.ToString();
        ErstelltVonRun.Text = aufgabe.ErstelltVon;
        ErstelltAmRun.Text = aufgabe.ErstelltAm.ToString("g");

        if (aufgabe.AbgeschlossenAm is { } abgeschlossen)
        {
            AbgeschlossenAmRun.Text = abgeschlossen.ToString("g");
        }
        else
        {
            AbgeschlossenAmPanel.Visibility = Visibility.Collapsed;
        }

        if (aufgabe.Status == AufgabeStatus.Fehlgeschlagen && !string.IsNullOrWhiteSpace(aufgabe.Fehlermeldung))
        {
            FehlermeldungText.Text = aufgabe.Fehlermeldung;
        }
        else
        {
            FehlermeldungPanel.Visibility = Visibility.Collapsed;
        }

        ParameterItemsControl.ItemsSource = aufgabe.ParameterWerte;
        ParameterItemsControl.Visibility = aufgabe.ParameterWerte.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SchliessenButton_Click(object sender, RoutedEventArgs e) => Close();
}
