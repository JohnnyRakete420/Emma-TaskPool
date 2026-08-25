using System.Windows;

namespace Emma.TrayApp;

/// <summary>
/// Kleines Bestätigungsfenster nach "An EMMA übergeben". Bestätigt nur, dass die Aufgabe im Pool
/// angelegt wurde - nicht, dass EMMA sie bereits erledigt hat (das kommt separat per Sprechblase).
/// </summary>
public partial class AufgabeUebergebenWindow : Window
{
    public AufgabeUebergebenWindow(bool erfolgreich, string prozessName, string? fehlermeldung = null)
    {
        InitializeComponent();

        if (erfolgreich)
        {
            IconText.Text = "✓";
            IconBorder.Background = (System.Windows.Media.Brush)FindResource("GreenBrush");
            TitelText.Text = "An EMMA übergeben";
            NachrichtText.Text = $"\"{prozessName}\" wurde in den Aufgabenpool aufgenommen. EMMA bearbeitet sie als Nächstes.";
        }
        else
        {
            IconText.Text = "✕";
            IconBorder.Background = (System.Windows.Media.Brush)FindResource("FehlerBrush");
            TitelText.Text = "Übergabe fehlgeschlagen";
            NachrichtText.Text = fehlermeldung ?? "Unbekannter Fehler.";
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => Close();
}
