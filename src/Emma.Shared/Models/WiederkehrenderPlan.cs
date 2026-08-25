namespace Emma.Shared.Models;

public class WiederkehrenderPlan
{
    public int Id { get; set; }
    public int ProzessId { get; set; }
    public Prozess? Prozess { get; set; }
    public bool Aktiv { get; set; } = true;

    /// <summary>
    /// JSON-Array der Zeitpunkte (Wochentag + Uhrzeit + letzte Ausführung), zu denen der
    /// Plan feuern soll - ein Plan kann mehrere Wochentage und pro Tag auch mehrere
    /// Uhrzeiten haben. Über <see cref="PlanJsonHelper"/> lesen/schreiben.
    /// </summary>
    public string ZeitpunkteJson { get; set; } = "[]";

    /// <summary>
    /// JSON-Array der Formularfelder (Bezeichnung + Wert), wird bei jeder automatisch erzeugten
    /// Aufgabe unverändert übernommen. Über <see cref="ParameterJsonHelper"/> lesen/schreiben.
    /// </summary>
    public string? ParameterJson { get; set; }
}
