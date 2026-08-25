using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Emma.TrayApp;

/// <summary>Rein lokale, geräteweise Einstellungen (nicht mit dem Service synchronisiert).</summary>
public record LokaleEinstellungenDaten(bool BenachrichtigungenAktiv = true);

/// <summary>
/// Liest/schreibt "einstellungen.json" unter %ProgramData%\EmmaAufgabenpool\Emma.TrayApp\ -
/// getrennt von emma-config.json (Server-Verbindungsdaten), weil es inhaltlich ein anderes
/// Anliegen ist: rein lokale UI-Präferenzen statt Zugangsdaten.
/// </summary>
public static class LokaleEinstellungen
{
    private const string DateiName = "einstellungen.json";

    public static LokaleEinstellungenDaten Lade()
    {
        var pfad = Pfad();
        if (!File.Exists(pfad))
            return new LokaleEinstellungenDaten();

        try
        {
            var json = File.ReadAllText(pfad);
            return JsonSerializer.Deserialize<LokaleEinstellungenDaten>(json) ?? new LokaleEinstellungenDaten();
        }
        catch
        {
            return new LokaleEinstellungenDaten();
        }
    }

    public static void Speichere(LokaleEinstellungenDaten daten)
    {
        try
        {
            var json = JsonSerializer.Serialize(daten, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Pfad(), json);
        }
        catch
        {
            // Kein Schreibzugriff - Einstellung gilt dann nur für die laufende Sitzung.
        }
    }

    private static string Pfad()
    {
        var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "EmmaAufgabenpool";
        var basis = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var ordner = Path.Combine(basis, "EmmaAufgabenpool", appName);

        try
        {
            Directory.CreateDirectory(ordner);
        }
        catch
        {
            return Path.Combine(AppContext.BaseDirectory, DateiName);
        }

        return Path.Combine(ordner, DateiName);
    }
}
