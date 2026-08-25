namespace Emma.Shared.Models;

public class Prozess
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Beschreibung { get; set; }

    /// <summary>
    /// JSON-Array der Feldbezeichnungen, die beim Anlegen einer Aufgabe für diesen Prozess
    /// abgefragt werden müssen (z.B. ["Vorname","Nachname","Abteilung"]). Leer/null = kein Formular nötig.
    /// Über <see cref="ParameterJsonHelper"/> lesen/schreiben.
    /// </summary>
    public string? ParameterFelderJson { get; set; }
}
