using Emma.Shared.Models;

namespace Emma.Shared.Dtos;

public record ProzessDto(
    int Id,
    string Name,
    string? Beschreibung,
    List<ParameterFeldDefinition> ParameterFelder);

public record AufgabeDto(
    int Id,
    int ProzessId,
    string ProzessName,
    AufgabeStatus Status,
    string ErstelltVon,
    DateTime ErstelltAm,
    DateTime? AbgeschlossenAm,
    List<ParameterFeldWert> ParameterWerte,
    string? Fehlermeldung);

public record NeueAufgabeRequest(int ProzessId, string ErstelltVon, List<ParameterFeldWert>? ParameterWerte);

public record FehlschlagRequest(string? Fehlermeldung);

public record WiederkehrenderPlanDto(
    int Id,
    int ProzessId,
    string ProzessName,
    bool Aktiv,
    List<PlanZeitpunkt> Zeitpunkte,
    List<ParameterFeldWert> ParameterWerte);

public record NeuerWiederkehrenderPlanRequest(int ProzessId, List<PlanZeitpunkt> Zeitpunkte, List<ParameterFeldWert>? ParameterWerte);
