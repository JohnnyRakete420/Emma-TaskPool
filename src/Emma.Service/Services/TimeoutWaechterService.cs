using Emma.Service.Data;
using Emma.Service.Security;
using Emma.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Emma.Service.Services;

/// <summary>
/// Prüft regelmäßig, ob Aufgaben zu lange in "Neu" oder "In Bearbeitung" hängen
/// (z.B. weil EMMA abgestürzt oder offline ist) und markiert sie nach Ablauf des
/// konfigurierten Timeouts automatisch als fehlgeschlagen, statt sie endlos offen zu lassen.
/// </summary>
public class TimeoutWaechterService(
    IServiceProvider services,
    ServiceKonfiguration konfig,
    ILogger<TimeoutWaechterService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PruefeUeberfaelligeAufgabenAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler beim Prüfen auf überfällige Aufgaben");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task PruefeUeberfaelligeAufgabenAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EmmaDbContext>();

        var grenze = DateTime.Now.AddMinutes(-konfig.TimeoutMinuten);

        var ueberfaellige = await db.Aufgaben
            .Where(a => (a.Status == AufgabeStatus.Neu || a.Status == AufgabeStatus.InBearbeitung)
                        && a.ErstelltAm < grenze)
            .ToListAsync(ct);

        foreach (var aufgabe in ueberfaellige)
        {
            aufgabe.Status = AufgabeStatus.Fehlgeschlagen;
            aufgabe.AbgeschlossenAm = DateTime.Now;
            aufgabe.Fehlermeldung =
                $"Zeitüberschreitung: keine Rückmeldung von EMMA innerhalb von {konfig.TimeoutMinuten} Minuten.";
            logger.LogWarning("Aufgabe {Id} wegen Zeitüberschreitung als fehlgeschlagen markiert", aufgabe.Id);
        }

        if (ueberfaellige.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
