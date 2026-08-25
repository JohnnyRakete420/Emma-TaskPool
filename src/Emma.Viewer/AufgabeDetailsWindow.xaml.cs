using System.Windows;
using Emma.Shared;

namespace Emma.Viewer;

/// <summary>
/// Zeigt die Formularwerte einer Aufgabe klar getrennt (ein Feld pro Zeile, große Schrift),
/// damit EMMA sie per Screen-Scan zuverlässig lesen und in ihre eigenen Variablen übernehmen kann -
/// statt einer zusammengequetschten Textzeile in der Tabelle.
/// </summary>
public partial class AufgabeDetailsWindow : Window
{
    public AufgabeDetailsWindow(string prozessName, List<ParameterFeldWert> werte)
    {
        InitializeComponent();
        ProzessNameText.Text = prozessName;
        FelderItemsControl.ItemsSource = werte;
    }

    private void WeiterButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
