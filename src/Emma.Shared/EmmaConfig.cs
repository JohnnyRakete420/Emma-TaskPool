using System.Reflection;
using System.Text.Json;

namespace Emma.Shared;

/// <summary>
/// Verbindungsdaten zum zentralen Emma.Service. ApiKey und ZertifikatThumbprint
/// müssen vom Admin aus der "fuer-clients.json" (liegt beim Service unter
/// %ProgramData%\EmmaAufgabenpool\Service\) in die emma-config.json des jeweiligen
/// Clients übernommen werden.
/// </summary>
public record EmmaClientConfig(string ServiceBaseUrl, string ApiKey, string? ZertifikatThumbprint);

/// <summary>
/// Liest die Verbindungsdaten aus einer "emma-config.json" unter
/// %ProgramData%\EmmaAufgabenpool\&lt;Appname&gt;\. Bewusst NICHT neben der .exe in
/// "Program Files" - Windows Installer räumt beim Upgrade eines MSI-Pakets unter
/// Umständen auch nicht-versionierte Dateien im Installationsordner mit auf, was die
/// Konfiguration bei jedem Update gelöscht hätte. %ProgramData% wird von MSI nie angefasst.
/// Existiert die Datei nicht, wird sie mit Platzhalterwerten angelegt - ohne gültigen
/// ApiKey/Zertifikats-Thumbprint schlagen Anfragen an den Service dann bewusst fehl.
/// </summary>
public static class EmmaConfig
{
    private const string DateiName = "emma-config.json";
    private const string StandardUrl = "https://localhost:5271/";

    public static EmmaClientConfig Lade()
    {
        var pfad = Path.Combine(KonfigurationsOrdner(), DateiName);
        MigriereAlteDateiFallsVorhanden(pfad);

        if (!File.Exists(pfad))
        {
            var standard = new EmmaClientConfig(StandardUrl, ApiKey: "", ZertifikatThumbprint: null);
            ErstelleStandardDatei(pfad, standard);
            return standard;
        }

        try
        {
            var json = File.ReadAllText(pfad);
            return JsonSerializer.Deserialize<EmmaClientConfig>(json)
                   ?? new EmmaClientConfig(StandardUrl, "", null);
        }
        catch
        {
            return new EmmaClientConfig(StandardUrl, "", null);
        }
    }

    /// <summary>%ProgramData%\EmmaAufgabenpool\&lt;Appname&gt;\ - je Anwendung ein eigener Unterordner.</summary>
    private static string KonfigurationsOrdner()
    {
        var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "EmmaAufgabenpool";
        var basis = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var ordner = Path.Combine(basis, "EmmaAufgabenpool", appName);

        try
        {
            Directory.CreateDirectory(ordner);
            return ordner;
        }
        catch
        {
            // Kein Zugriff auf ProgramData (z.B. Rechte-Vererbung, Gruppenrichtlinie, Virenscanner) -
            // Fallback neben die .exe, damit die Anwendung auf jeden Fall startet statt abzustürzen.
            return AppContext.BaseDirectory;
        }
    }

    /// <summary>
    /// Einmalige Übernahme einer emma-config.json aus einer älteren Version, die noch
    /// neben der .exe lag - damit niemand die Werte nach einem Update erneut eintippen muss.
    /// </summary>
    private static void MigriereAlteDateiFallsVorhanden(string neuerPfad)
    {
        if (File.Exists(neuerPfad))
            return;

        try
        {
            var alterPfad = Path.Combine(AppContext.BaseDirectory, DateiName);
            if (File.Exists(alterPfad))
                File.Copy(alterPfad, neuerPfad);
        }
        catch
        {
            // Keine alte Datei vorhanden oder kein Zugriff - kein Problem, Standarddatei wird angelegt.
        }
    }

    private static void ErstelleStandardDatei(string pfad, EmmaClientConfig standard)
    {
        try
        {
            var json = JsonSerializer.Serialize(standard, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(pfad, json);
        }
        catch
        {
            // Kein Schreibzugriff - Standardwert wird trotzdem verwendet.
        }
    }

    /// <summary>Schreibt geänderte Verbindungsdaten zurück (z.B. aus dem Einstellungen-Fenster).</summary>
    public static void Speichere(EmmaClientConfig config)
    {
        var pfad = Path.Combine(KonfigurationsOrdner(), DateiName);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(pfad, json);
    }
}
