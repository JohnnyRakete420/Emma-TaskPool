# EMMA Aufgabenpool

Desktop-Anwendung, mit der Mitarbeiter automatisierbare Prozesse an **EMMA** (Wianco) übergeben können – manuell per Klick oder automatisch nach Zeitplan. EMMA erkennt neue Aufgaben rein visuell (Screen-Scan/Klick), daher stellt das System ihr einen eigenen Bildschirm mit Aufgabenliste bereit, statt sie über eine klassische API anzusprechen.

## Was das ist

Drei Programme + ein zentraler Dienst:

| Komponente | Läuft bei | Zweck |
|---|---|---|
| **Emma.Service** | zentraler EMMA-Rechner | Zentrale API + Datenbank, einzige "Quelle der Wahrheit" für alle Aufgaben |
| **Emma.TrayApp** | jedem Arbeitsplatz | Tray-Icon zum Anlegen von Aufgaben und wiederkehrenden Plänen, Verlauf/Übersicht, Benachrichtigung bei Abschluss |
| **Emma.Viewer** | EMMA-Rechner | Das Fenster, das EMMA per Screen-Scan beobachtet: offene Aufgaben lesen, nach Ausführung als erledigt/fehlgeschlagen markieren |
| **Emma.Shared** | – | Gemeinsame Datenmodelle/DTOs und der HTTP-Client, den TrayApp und Viewer nutzen |

**Ablauf:** Benutzer wählt in der TrayApp einen Prozess → Aufgabe erscheint im Emma.Viewer-Fenster auf dem EMMA-Rechner → EMMA liest den Prozessnamen, führt ihren dazu passenden Ablaufplan aus, klickt danach im Viewer auf "Erledigt" (oder "Fehlgeschlagen") → TrayApp des Erstellers zeigt eine Benachrichtigung.

## Schnellstart (Entwicklung)

Voraussetzung: .NET 10 SDK, Windows (WPF).

```bash
cd Emma-Aufgabenpool
dotnet build
```

Service starten (erzeugt beim allerersten Start automatisch API-Key + selbstsigniertes Zertifikat):

```bash
dotnet run --project src/Emma.Service
```

TrayApp und Viewer aus `src/Emma.TrayApp/bin/Debug/net10.0-windows/` bzw. `src/Emma.Viewer/bin/Debug/net10.0-windows/` starten. Beim ersten Start legen sie eine `emma-config.json` mit Platzhalterwerten an – siehe [Konfiguration](#konfiguration), damit sie den Service tatsächlich erreichen.

## Architektur

```
┌─────────────────┐        HTTPS (Zertifikats-Pinning)        ┌──────────────────┐
│  Emma.TrayApp    │ ───────────────────────────────────────▶ │                  │
│  (jeder Arbeits-  │ ◀─────────────────────────────────────── │   Emma.Service   │
│   platz)          │        X-Api-Key Header                  │ (zentraler        │
└─────────────────┘                                            │  EMMA-Rechner)    │
                                                                 │                  │
┌─────────────────┐        HTTPS (Zertifikats-Pinning)        │  SQLite-DB       │
│  Emma.Viewer      │ ───────────────────────────────────────▶ │  Scheduler       │
│  (EMMA-Rechner,   │ ◀─────────────────────────────────────── │  Timeout-Wächter │
│   von EMMA         │        X-Api-Key Header                  │                  │
│   beobachtet)      │                                          └──────────────────┘
└─────────────────┘
```

- **Emma.Service**: ASP.NET Core Minimal API, SQLite via EF Core, läuft auf Port `5271` über HTTPS.
- **Kein klassisches Client-Auth für EMMA selbst** – EMMA interagiert nur visuell mit dem Emma.Viewer-Fenster, nie direkt mit der API.
- **Zwei Hintergrunddienste** im Service:
  - `WiederkehrendePlaeneService` – prüft jede Minute, ob ein wiederkehrender Plan fällig ist, legt dann automatisch eine Aufgabe an.
  - `TimeoutWaechterService` – prüft alle 5 Minuten auf Aufgaben, die zu lange in "Neu"/"In Bearbeitung" hängen (Standard: 30 Min.), und markiert sie automatisch als fehlgeschlagen.

### Datenmodell

- **Prozess**: `Name`, `Beschreibung`, optional `BenoetigtParameter` + `ParameterBezeichnung` (z.B. "Datum") für Prozesse, die eine Zusatzangabe brauchen.
- **Aufgabe**: `Status` (`Neu` / `InBearbeitung` / `Erledigt` / `Fehlgeschlagen`), `ErstelltVon`, `ErstelltAm`, `AbgeschlossenAm`, `Parameter`, `Fehlermeldung`.
- **WiederkehrenderPlan**: `Wochentag`, `Uhrzeit`, `Aktiv`, `Parameter` (falls der Zielprozess einen braucht).

Schema-Änderungen laufen über **EF Core Migrationen** (`src/Emma.Service/Data/Migrations`), nicht über `EnsureCreated()` – bestehende Daten bleiben beim Aktualisieren erhalten.

## Sicherheit

- **HTTPS**: Der Service erzeugt beim allerersten Start ein selbstsigniertes Zertifikat (5 Jahre gültig).
- **Zertifikats-Pinning statt Windows-Zertifikatsspeicher**: Clients prüfen den exakten Thumbprint aus ihrer `emma-config.json`, statt dem Zertifikat pauschal zu vertrauen. Es muss also nichts in den Windows-Zertifikatsspeicher eingetragen werden.
- **API-Key**: Zufällig generiert, wird bei jeder Anfrage im Header `X-Api-Key` erwartet (zeitkonstanter Vergleich gegen Timing-Angriffe).
- Details zur Verteilung der Zugangsdaten an neue Clients: siehe [BETRIEB.md](BETRIEB.md).

## Projektstruktur

```
Emma-Aufgabenpool/
├── EmmaAufgabenpool.sln
└── src/
    ├── Emma.Shared/        Datenmodelle, DTOs, EmmaApiClient, EmmaConfig
    ├── Emma.Service/       API, DB-Kontext, Migrationen, Hintergrunddienste, Zertifikat/API-Key-Erzeugung
    ├── Emma.TrayApp/       Tray-Icon-App (Prozessauswahl, Pläne, Verlauf, Autostart)
    └── Emma.Viewer/        EMMA-facing Fenster
```

## Konfiguration

Jede Client-Anwendung (TrayApp, Viewer) liest eine `emma-config.json` neben ihrer `.exe`:

```json
{
  "ServiceBaseUrl": "https://<emma-rechner>:5271/",
  "ApiKey": "...",
  "ZertifikatThumbprint": "..."
}
```

Die passenden Werte generiert der Service beim ersten Start automatisch in `fuer-clients.json` (liegt neben `Emma.Service.dll`) – siehe [BETRIEB.md](BETRIEB.md) für den genauen Ablauf.

Server-seitige Einstellungen (API-Key, Zertifikats-Passwort, Timeout-Minuten) liegen in `emma-service-config.json` neben der Service-.exe.

## Weiterführend

- [BETRIEB.md](BETRIEB.md) – Aufsetzen auf dem EMMA-Rechner, neue Clients anbinden, Fehlersuche, laufende Wartung.
