namespace Emma.Shared.Models;

public class Aufgabe
{
    public int Id { get; set; }
    public int ProzessId { get; set; }
    public Prozess? Prozess { get; set; }
    public AufgabeStatus Status { get; set; } = AufgabeStatus.Neu;
    public string ErstelltVon { get; set; } = "";
    public DateTime ErstelltAm { get; set; } = DateTime.Now;
    /// <summary>Zeitpunkt des Abschlusses - bei Erfolg wie bei Fehlschlag gesetzt.</summary>
    public DateTime? AbgeschlossenAm { get; set; }

    /// <summary>
    /// JSON-Array der ausgefüllten Formularfelder (Bezeichnung + Wert), passend zu
    /// <see cref="Prozess.ParameterFelderJson"/>. Über <see cref="ParameterJsonHelper"/> lesen/schreiben.
    /// </summary>
    public string? ParameterJson { get; set; }

    /// <summary>Nur gesetzt, wenn Status == Fehlgeschlagen.</summary>
    public string? Fehlermeldung { get; set; }
}
