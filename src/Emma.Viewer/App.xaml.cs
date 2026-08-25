using System.Configuration;
using System.Data;
using System.Windows;
using Emma.Shared;

namespace Emma.Viewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static readonly EmmaClientConfig Config = EmmaConfig.Lade();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Sicherheitsnetz: ein unerwarteter Fehler soll eine Meldung zeigen statt die
        // Anwendung stillschweigend abstürzen zu lassen.
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Unerwarteter Fehler: {args.Exception.Message}",
                "EMMA Viewer", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}

