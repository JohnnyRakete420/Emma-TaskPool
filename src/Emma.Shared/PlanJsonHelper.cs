using System.Text.Json;

namespace Emma.Shared;

/// <summary>
/// Ein Zeitpunkt eines wiederkehrenden Plans. Ein Plan kann mehrere davon haben
/// (z.B. Montag 20:00 Uhr UND Mittwoch 08:00 Uhr UND Mittwoch 20:00 Uhr).
/// </summary>
public record PlanZeitpunkt(DayOfWeek Wochentag, TimeOnly Uhrzeit, DateOnly? LetzteAusfuehrung = null);

/// <summary>
/// Wandelt zwischen der ZeitpunkteJson-Spalte in der DB (WiederkehrenderPlan) und der
/// entsprechenden .NET-Liste um. Zentral an einer Stelle, damit Server und Clients
/// dasselbe Format nutzen.
/// </summary>
public static class PlanJsonHelper
{
    public static string SerializeZeitpunkte(List<PlanZeitpunkt> zeitpunkte) =>
        JsonSerializer.Serialize(zeitpunkte);

    public static List<PlanZeitpunkt> DeserializeZeitpunkte(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<PlanZeitpunkt>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static readonly Dictionary<DayOfWeek, string> Wochentagsnamen = new()
    {
        [DayOfWeek.Monday] = "Montag",
        [DayOfWeek.Tuesday] = "Dienstag",
        [DayOfWeek.Wednesday] = "Mittwoch",
        [DayOfWeek.Thursday] = "Donnerstag",
        [DayOfWeek.Friday] = "Freitag",
        [DayOfWeek.Saturday] = "Samstag",
        [DayOfWeek.Sunday] = "Sonntag",
    };

    private static readonly DayOfWeek[] WochentagsReihenfolge =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    public static string DeutscherWochentag(DayOfWeek tag) => Wochentagsnamen[tag];

    public static string FormatiereUhrzeit(TimeOnly uhrzeit) => $"{uhrzeit:HH\\:mm} Uhr";

    /// <summary>Für die Anzeige in Tabellen: "Montag 20:00 Uhr · Mittwoch 08:00 Uhr, 20:00 Uhr".</summary>
    public static string FormatiereZeitpunkte(List<PlanZeitpunkt> zeitpunkte)
    {
        if (zeitpunkte.Count == 0)
            return "";

        var gruppen = zeitpunkte
            .GroupBy(z => z.Wochentag)
            .OrderBy(g => Array.IndexOf(WochentagsReihenfolge, g.Key))
            .Select(g => $"{DeutscherWochentag(g.Key)} {string.Join(", ", g.OrderBy(z => z.Uhrzeit).Select(z => FormatiereUhrzeit(z.Uhrzeit)))}");

        return string.Join(" · ", gruppen);
    }
}
