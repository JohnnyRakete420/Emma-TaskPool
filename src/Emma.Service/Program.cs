using System.Security.Cryptography;
using Emma.Service.Data;
using Emma.Service.Security;
using Emma.Service.Services;
using Emma.Shared;
using Emma.Shared.Dtos;
using Emma.Shared.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Läuft sowohl als Konsolen-App (Entwicklung) als auch als registrierter
// Windows-Dienst (Produktion) - erkennt den Kontext automatisch.
builder.Host.UseWindowsService(options => options.ServiceName = "EmmaAufgabenpoolService");

// %ProgramData%\EmmaAufgabenpool\Service\ statt neben der .exe in "Program Files": MSI-Upgrades
// räumen dort unter Umständen auch nicht-versionierte Dateien auf, das würde eure Aufgaben-
// Datenbank bei jedem Update leeren. %ProgramData% wird von MSI nie angefasst.
var datenOrdner = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EmmaAufgabenpool", "Service");
try
{
    Directory.CreateDirectory(datenOrdner);
}
catch
{
    // Kein Zugriff auf ProgramData - Fallback neben die .exe, damit der Dienst trotzdem startet.
    datenOrdner = AppContext.BaseDirectory;
}

var dbPath = Path.Combine(datenOrdner, "emma-aufgabenpool.db");
foreach (var endung in new[] { "", "-shm", "-wal" })
{
    var neuerPfad = dbPath + endung;
    var alterPfad = Path.Combine(AppContext.BaseDirectory, "emma-aufgabenpool.db" + endung);
    if (!File.Exists(neuerPfad) && File.Exists(alterPfad))
        File.Copy(alterPfad, neuerPfad);
}

builder.Services.AddDbContext<EmmaDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddHostedService<WiederkehrendePlaeneService>();
builder.Services.AddHostedService<TimeoutWaechterService>();

// API-Key + selbstsigniertes Zertifikat laden (oder beim allerersten Start erzeugen),
// bevor Kestrel gebaut wird, damit HTTPS direkt mit dem Zertifikat konfiguriert werden kann.
using var bootstrapLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
var (sicherheitsKonfig, zertifikat) = ServiceSecurity.LadeOderErstelle(bootstrapLoggerFactory.CreateLogger("Emma.Service.Sicherheit"));
builder.Services.AddSingleton(sicherheitsKonfig);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5271, listenOptions => listenOptions.UseHttps(zertifikat));
});

var app = builder.Build();

// API-Key-Prüfung für alle /api/-Routen. Ohne gültigen X-Api-Key-Header: 401.
var erwarteterApiKeyBytes = System.Text.Encoding.UTF8.GetBytes(sicherheitsKonfig.ApiKey);
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var uebergebenerKey = context.Request.Headers["X-Api-Key"].ToString();
        var uebergebenerKeyBytes = System.Text.Encoding.UTF8.GetBytes(uebergebenerKey);

        var gueltig = uebergebenerKeyBytes.Length == erwarteterApiKeyBytes.Length
            && CryptographicOperations.FixedTimeEquals(uebergebenerKeyBytes, erwarteterApiKeyBytes);

        if (!gueltig)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Ungültiger oder fehlender API-Key.");
            return;
        }
    }

    await next();
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EmmaDbContext>();
    db.Database.Migrate();
    SeedData.SeedProzesse(db);
}

// Prüft, ob für alle Formularfelder des Prozesses ein nicht-leerer Wert mitgeschickt wurde und dass
// Auswahl-/Mehrfachauswahl-Felder nur zulässige Optionen enthalten. Gibt bei Verstoß eine
// Fehlermeldung zurück, sonst null.
string? PruefeParameterFelder(Prozess prozess, List<ParameterFeldWert>? werte)
{
    var benoetigteFelder = ParameterJsonHelper.DeserializeFelder(prozess.ParameterFelderJson);
    if (benoetigteFelder.Count == 0)
        return null;

    var fehlende = benoetigteFelder
        .Where(feld => string.IsNullOrWhiteSpace(werte?.FirstOrDefault(w => w.Bezeichnung == feld.Bezeichnung)?.Wert))
        .Select(feld => feld.Bezeichnung)
        .ToList();

    if (fehlende.Count > 0)
        return $"Prozess \"{prozess.Name}\" benötigt eine Angabe für: {string.Join(", ", fehlende)}.";

    foreach (var feld in benoetigteFelder.Where(f => f.Typ is ParameterFeldTyp.Auswahl or ParameterFeldTyp.Mehrfachauswahl))
    {
        var wert = werte!.First(w => w.Bezeichnung == feld.Bezeichnung).Wert;
        var ausgewaehlt = feld.Typ == ParameterFeldTyp.Mehrfachauswahl
            ? wert.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [wert.Trim()];

        var unbekannt = ausgewaehlt.Where(a => !(feld.Optionen ?? []).Contains(a)).ToList();
        if (unbekannt.Count > 0)
            return $"Ungültige Auswahl bei \"{feld.Bezeichnung}\": {string.Join(", ", unbekannt)}.";
    }

    return null;
}

// ---- Prozesse ----

// Prüft Name (Pflicht, eindeutig) und Felddefinitionen (Bezeichnung Pflicht+eindeutig,
// Auswahl/Mehrfachauswahl brauchen mindestens eine Option). Gibt bei Verstoß eine
// Fehlermeldung zurück, sonst null. "ignoriereProzessId" schließt beim Bearbeiten den
// eigenen Prozess von der Namens-Eindeutigkeitsprüfung aus.
async Task<string?> PruefeProzessAsync(EmmaDbContext db, string name, List<ParameterFeldDefinition> felder, int? ignoriereProzessId)
{
    if (string.IsNullOrWhiteSpace(name))
        return "Bitte einen Namen angeben.";

    var nameVergeben = await db.Prozesse
        .AnyAsync(p => p.Name.ToLower() == name.Trim().ToLower() && p.Id != (ignoriereProzessId ?? -1));
    if (nameVergeben)
        return $"Ein Prozess mit dem Namen \"{name.Trim()}\" existiert bereits.";

    var leereBezeichnung = felder.Any(f => string.IsNullOrWhiteSpace(f.Bezeichnung));
    if (leereBezeichnung)
        return "Jedes Formularfeld benötigt eine Bezeichnung.";

    var doppelteBezeichnung = felder.Select(f => f.Bezeichnung.Trim().ToLower()).GroupBy(b => b).Any(g => g.Count() > 1);
    if (doppelteBezeichnung)
        return "Formularfeld-Bezeichnungen müssen innerhalb eines Prozesses eindeutig sein.";

    var fehlendeOptionen = felder.FirstOrDefault(f =>
        f.Typ is ParameterFeldTyp.Auswahl or ParameterFeldTyp.Mehrfachauswahl
        && (f.Optionen is null || f.Optionen.Count(o => !string.IsNullOrWhiteSpace(o)) == 0));
    if (fehlendeOptionen is not null)
        return $"Feld \"{fehlendeOptionen.Bezeichnung}\" benötigt mindestens eine Auswahl-Option.";

    return null;
}

// Text-Felder brauchen keine Optionen - werden hier bereinigt, damit sie nicht versehentlich
// mitgespeichert werden (z.B. wenn ein Feld im Formular von Auswahl auf Text umgestellt wurde).
List<ParameterFeldDefinition> BereinigeFelder(List<ParameterFeldDefinition> felder) =>
    felder.Select(f => f.Typ == ParameterFeldTyp.Text ? f with { Optionen = null } : f).ToList();

app.MapGet("/api/prozesse", async (EmmaDbContext db) =>
{
    var prozesse = await db.Prozesse.ToListAsync();
    return prozesse
        .Select(p => new ProzessDto(p.Id, p.Name, p.Beschreibung, ParameterJsonHelper.DeserializeFelder(p.ParameterFelderJson)))
        .ToList();
});

app.MapPost("/api/prozesse", async (EmmaDbContext db, NeuerProzessRequest request) =>
{
    var felder = BereinigeFelder(request.ParameterFelder ?? []);
    var fehler = await PruefeProzessAsync(db, request.Name, felder, ignoriereProzessId: null);
    if (fehler is not null)
        return Results.BadRequest(fehler);

    var prozess = new Prozess
    {
        Name = request.Name.Trim(),
        Beschreibung = string.IsNullOrWhiteSpace(request.Beschreibung) ? null : request.Beschreibung.Trim(),
        ParameterFelderJson = ParameterJsonHelper.SerializeFelder(felder)
    };
    db.Prozesse.Add(prozess);
    await db.SaveChangesAsync();

    var dto = new ProzessDto(prozess.Id, prozess.Name, prozess.Beschreibung, felder);
    return Results.Created($"/api/prozesse/{prozess.Id}", dto);
});

app.MapPut("/api/prozesse/{id:int}", async (EmmaDbContext db, int id, NeuerProzessRequest request) =>
{
    var prozess = await db.Prozesse.FindAsync(id);
    if (prozess is null)
        return Results.NotFound();

    var felder = BereinigeFelder(request.ParameterFelder ?? []);
    var fehler = await PruefeProzessAsync(db, request.Name, felder, ignoriereProzessId: id);
    if (fehler is not null)
        return Results.BadRequest(fehler);

    prozess.Name = request.Name.Trim();
    prozess.Beschreibung = string.IsNullOrWhiteSpace(request.Beschreibung) ? null : request.Beschreibung.Trim();
    prozess.ParameterFelderJson = ParameterJsonHelper.SerializeFelder(felder);
    await db.SaveChangesAsync();

    return Results.Ok(new ProzessDto(prozess.Id, prozess.Name, prozess.Beschreibung, felder));
});

app.MapDelete("/api/prozesse/{id:int}", async (EmmaDbContext db, int id) =>
{
    var prozess = await db.Prozesse.FindAsync(id);
    if (prozess is null)
        return Results.NotFound();

    var hatVerlauf = await db.Aufgaben.AnyAsync(a => a.ProzessId == id);
    var hatPlaene = await db.WiederkehrendePlaene.AnyAsync(p => p.ProzessId == id);
    if (hatVerlauf || hatPlaene)
        return Results.BadRequest(
            "Dieser Prozess hat bereits Aufgaben-Verlauf oder wiederkehrende Pläne und kann deshalb nicht gelöscht werden.");

    db.Prozesse.Remove(prozess);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// ---- Aufgaben ----

app.MapGet("/api/aufgaben", async (EmmaDbContext db, AufgabeStatus? status) =>
{
    var query = db.Aufgaben.Include(a => a.Prozess).AsQueryable();
    if (status is not null)
        query = query.Where(a => a.Status == status);

    var aufgaben = await query.OrderBy(a => a.ErstelltAm).ToListAsync();
    return aufgaben
        .Select(a => new AufgabeDto(
            a.Id, a.ProzessId, a.Prozess!.Name, a.Status, a.ErstelltVon, a.ErstelltAm,
            a.AbgeschlossenAm, ParameterJsonHelper.DeserializeWerte(a.ParameterJson), a.Fehlermeldung))
        .ToList();
});

app.MapPost("/api/aufgaben", async (EmmaDbContext db, NeueAufgabeRequest request) =>
{
    var prozess = await db.Prozesse.FindAsync(request.ProzessId);
    if (prozess is null)
        return Results.NotFound($"Prozess {request.ProzessId} nicht gefunden.");

    var fehler = PruefeParameterFelder(prozess, request.ParameterWerte);
    if (fehler is not null)
        return Results.BadRequest(fehler);

    var aufgabe = new Aufgabe
    {
        ProzessId = request.ProzessId,
        ErstelltVon = request.ErstelltVon,
        Status = AufgabeStatus.Neu,
        ErstelltAm = DateTime.Now,
        ParameterJson = ParameterJsonHelper.SerializeWerte(request.ParameterWerte)
    };
    db.Aufgaben.Add(aufgabe);
    await db.SaveChangesAsync();

    var dto = new AufgabeDto(
        aufgabe.Id, aufgabe.ProzessId, prozess.Name, aufgabe.Status, aufgabe.ErstelltVon, aufgabe.ErstelltAm,
        aufgabe.AbgeschlossenAm, ParameterJsonHelper.DeserializeWerte(aufgabe.ParameterJson), aufgabe.Fehlermeldung);
    return Results.Created($"/api/aufgaben/{aufgabe.Id}", dto);
});

app.MapPatch("/api/aufgaben/{id:int}/erledigt", async (EmmaDbContext db, int id) =>
{
    var aufgabe = await db.Aufgaben.FindAsync(id);
    if (aufgabe is null)
        return Results.NotFound();

    aufgabe.Status = AufgabeStatus.Erledigt;
    aufgabe.AbgeschlossenAm = DateTime.Now;
    aufgabe.Fehlermeldung = null;
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPatch("/api/aufgaben/{id:int}/in-bearbeitung", async (EmmaDbContext db, int id) =>
{
    var aufgabe = await db.Aufgaben.FindAsync(id);
    if (aufgabe is null)
        return Results.NotFound();

    aufgabe.Status = AufgabeStatus.InBearbeitung;
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPatch("/api/aufgaben/{id:int}/fehlgeschlagen", async (EmmaDbContext db, int id, FehlschlagRequest request) =>
{
    var aufgabe = await db.Aufgaben.FindAsync(id);
    if (aufgabe is null)
        return Results.NotFound();

    aufgabe.Status = AufgabeStatus.Fehlgeschlagen;
    aufgabe.AbgeschlossenAm = DateTime.Now;
    aufgabe.Fehlermeldung = string.IsNullOrWhiteSpace(request.Fehlermeldung)
        ? "Von EMMA als fehlgeschlagen markiert."
        : request.Fehlermeldung;
    await db.SaveChangesAsync();
    return Results.Ok();
});

// ---- Wiederkehrende Pläne ----

// Prüft, dass mindestens ein Zeitpunkt angegeben wurde. Gibt bei Verstoß eine Fehlermeldung zurück, sonst null.
string? PruefeZeitpunkte(List<PlanZeitpunkt>? zeitpunkte) =>
    zeitpunkte is null || zeitpunkte.Count == 0
        ? "Bitte mindestens einen Wochentag mit Uhrzeit angeben."
        : null;

WiederkehrenderPlanDto ZuPlanDto(WiederkehrenderPlan plan, string prozessName) => new(
    plan.Id, plan.ProzessId, prozessName, plan.Aktiv,
    PlanJsonHelper.DeserializeZeitpunkte(plan.ZeitpunkteJson),
    ParameterJsonHelper.DeserializeWerte(plan.ParameterJson));

app.MapGet("/api/wiederkehrende-plaene", async (EmmaDbContext db) =>
{
    var plaene = await db.WiederkehrendePlaene.Include(p => p.Prozess).ToListAsync();
    return plaene.Select(p => ZuPlanDto(p, p.Prozess!.Name)).ToList();
});

app.MapPost("/api/wiederkehrende-plaene", async (EmmaDbContext db, NeuerWiederkehrenderPlanRequest request) =>
{
    var prozess = await db.Prozesse.FindAsync(request.ProzessId);
    if (prozess is null)
        return Results.NotFound($"Prozess {request.ProzessId} nicht gefunden.");

    var zeitFehler = PruefeZeitpunkte(request.Zeitpunkte);
    if (zeitFehler is not null)
        return Results.BadRequest(zeitFehler);

    var fehler = PruefeParameterFelder(prozess, request.ParameterWerte);
    if (fehler is not null)
        return Results.BadRequest(fehler);

    var plan = new WiederkehrenderPlan
    {
        ProzessId = request.ProzessId,
        Aktiv = true,
        ZeitpunkteJson = PlanJsonHelper.SerializeZeitpunkte(request.Zeitpunkte),
        ParameterJson = ParameterJsonHelper.SerializeWerte(request.ParameterWerte)
    };
    db.WiederkehrendePlaene.Add(plan);
    await db.SaveChangesAsync();

    return Results.Created($"/api/wiederkehrende-plaene/{plan.Id}", ZuPlanDto(plan, prozess.Name));
});

app.MapPut("/api/wiederkehrende-plaene/{id:int}", async (EmmaDbContext db, int id, NeuerWiederkehrenderPlanRequest request) =>
{
    var plan = await db.WiederkehrendePlaene.FindAsync(id);
    if (plan is null)
        return Results.NotFound();

    var prozess = await db.Prozesse.FindAsync(request.ProzessId);
    if (prozess is null)
        return Results.NotFound($"Prozess {request.ProzessId} nicht gefunden.");

    var zeitFehler = PruefeZeitpunkte(request.Zeitpunkte);
    if (zeitFehler is not null)
        return Results.BadRequest(zeitFehler);

    var fehler = PruefeParameterFelder(prozess, request.ParameterWerte);
    if (fehler is not null)
        return Results.BadRequest(fehler);

    plan.ProzessId = request.ProzessId;
    plan.ZeitpunkteJson = PlanJsonHelper.SerializeZeitpunkte(request.Zeitpunkte);
    plan.ParameterJson = ParameterJsonHelper.SerializeWerte(request.ParameterWerte);
    await db.SaveChangesAsync();

    return Results.Ok(ZuPlanDto(plan, prozess.Name));
});

app.MapDelete("/api/wiederkehrende-plaene/{id:int}", async (EmmaDbContext db, int id) =>
{
    var plan = await db.WiederkehrendePlaene.FindAsync(id);
    if (plan is null)
        return Results.NotFound();

    db.WiederkehrendePlaene.Remove(plan);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// Kein URL-Parameter mehr nötig: Endpoint (0.0.0.0:5271, HTTPS) ist bereits über
// ConfigureKestrel oben mit dem Zertifikat gesetzt.
app.Run();
