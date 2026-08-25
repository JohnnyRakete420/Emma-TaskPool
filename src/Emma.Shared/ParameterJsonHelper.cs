using System.Text.Json;

namespace Emma.Shared;

/// <summary>Ein ausgefülltes Formularfeld (z.B. "Vorname" -> "Max").</summary>
public record ParameterFeldWert(string Bezeichnung, string Wert);

/// <summary>Wie ein Formularfeld eingegeben wird.</summary>
public enum ParameterFeldTyp
{
    /// <summary>Freitext (Standard).</summary>
    Text,
    /// <summary>Dropdown - genau eine Option aus <see cref="ParameterFeldDefinition.Optionen"/>.</summary>
    Auswahl,
    /// <summary>Häkchen - beliebig viele Optionen aus <see cref="ParameterFeldDefinition.Optionen"/>.</summary>
    Mehrfachauswahl
}

/// <summary>Definition eines Formularfelds eines Prozesses (Bezeichnung + Eingabeart + ggf. Auswahloptionen).</summary>
public record ParameterFeldDefinition(string Bezeichnung, ParameterFeldTyp Typ = ParameterFeldTyp.Text, List<string>? Optionen = null);

/// <summary>
/// Wandelt zwischen den JSON-Textspalten in der DB (Prozess.ParameterFelderJson,
/// Aufgabe.ParameterJson, WiederkehrenderPlan.ParameterJson) und den entsprechenden
/// .NET-Listen um. Zentral an einer Stelle, damit Server und Clients dasselbe Format nutzen.
/// </summary>
public static class ParameterJsonHelper
{
    public static string? SerializeFelder(List<ParameterFeldDefinition>? felder) =>
        felder is null || felder.Count == 0 ? null : JsonSerializer.Serialize(felder);

    public static List<ParameterFeldDefinition> DeserializeFelder(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ParameterFeldDefinition>>(json) ?? [];
        }
        catch (JsonException)
        {
            // Schützt gegen nicht-JSON-Altdaten (z.B. aus einer Version vor dem Formular-Modell).
            return [];
        }
    }

    public static string? SerializeWerte(List<ParameterFeldWert>? werte) =>
        werte is null || werte.Count == 0 ? null : JsonSerializer.Serialize(werte);

    public static List<ParameterFeldWert> DeserializeWerte(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ParameterFeldWert>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>Für die Anzeige in Tabellen: "Vorname: Max, Nachname: Mustermann".</summary>
    public static string FormatiereWerte(List<ParameterFeldWert>? werte) =>
        werte is null || werte.Count == 0
            ? ""
            : string.Join(", ", werte.Select(w => $"{w.Bezeichnung}: {w.Wert}"));
}
