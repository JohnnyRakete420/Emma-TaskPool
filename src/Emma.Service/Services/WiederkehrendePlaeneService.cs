using Emma.Service.Data;
using Emma.Shared;
using Emma.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Emma.Service.Services;

/// <summary>
/// Prüft minütlich, ob einer der Zeitpunkte eines wiederkehrenden Plans (z.B. Kassenautomat,
/// jeden Mittwoch 20 Uhr) fällig ist, und legt dafür automatisch eine neue Aufgabe im Pool an.
/// Ein Plan kann mehrere Wochentage und pro Tag auch mehrere Uhrzeiten haben - jeder Zeitpunkt
/// wird unabhängig von den anderen verfolgt (eigene "letzte Ausführung").
/// </summary>
public class WiederkehrendePlaeneService(IServiceProvider services, ILogger<WiederkehrendePlaeneService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PruefeUndErstelleAufgabenAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler beim Prüfen der wiederkehrenden Pläne");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task PruefeUndErstelleAufgabenAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EmmaDbContext>();

        var jetzt = DateTime.Now;
        var heute = DateOnly.FromDateTime(jetzt);
        var aktuelleUhrzeit = TimeOnly.FromDateTime(jetzt);

        var aktivePlaene = await db.WiederkehrendePlaene.Where(p => p.Aktiv).ToListAsync(ct);
        var geaendert = false;

        foreach (var plan in aktivePlaene)
        {
            var zeitpunkte = PlanJsonHelper.DeserializeZeitpunkte(plan.ZeitpunkteJson);
            var aktualisiert = false;

            for (var i = 0; i < zeitpunkte.Count; i++)
            {
                var zeitpunkt = zeitpunkte[i];
                var faellig = zeitpunkt.Wochentag == jetzt.DayOfWeek
                    && zeitpunkt.Uhrzeit <= aktuelleUhrzeit
                    // Nur Zeitpunkte, deren Fälligkeit innerhalb der letzten 2 Minuten liegt (sonst
                    // würde ein verpasster Lauf z.B. nach Neustart sofort um Mitternacht nachgeholt).
                    && aktuelleUhrzeit - zeitpunkt.Uhrzeit <= TimeSpan.FromMinutes(2)
                    && zeitpunkt.LetzteAusfuehrung != heute;

                if (!faellig)
                    continue;

                db.Aufgaben.Add(new Aufgabe
                {
                    ProzessId = plan.ProzessId,
                    Status = AufgabeStatus.Neu,
                    ErstelltVon = "System (wiederkehrend)",
                    ErstelltAm = jetzt,
                    ParameterJson = plan.ParameterJson
                });

                zeitpunkte[i] = zeitpunkt with { LetzteAusfuehrung = heute };
                aktualisiert = true;
                logger.LogInformation(
                    "Wiederkehrende Aufgabe für Plan {PlanId} ({Wochentag} {Uhrzeit}) erstellt",
                    plan.Id, zeitpunkt.Wochentag, zeitpunkt.Uhrzeit);
            }

            if (aktualisiert)
            {
                plan.ZeitpunkteJson = PlanJsonHelper.SerializeZeitpunkte(zeitpunkte);
                geaendert = true;
            }
        }

        if (geaendert)
            await db.SaveChangesAsync(ct);
    }
}
