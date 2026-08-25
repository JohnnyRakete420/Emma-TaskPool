# Betriebsleitfaden – EMMA Aufgabenpool

Für IT-Kollegen, die den Emma.Service betreiben und neue Arbeitsplätze anbinden. Für die Architektur-Übersicht siehe [README.md](README.md).

## Voraussetzungen

- Zugriff auf den vorgesehenen EMMA-Rechner (zentraler Dienst läuft dort)
- Lokale Administratorrechte auf jedem Zielrechner (die MSIs installieren nach Program Files und registrieren ggf. einen Windows-Dienst)
- Zum **Bauen** der Installer: .NET 10 SDK + [WiX Toolset](https://wixtoolset.org/) v5 (`dotnet tool install --global wix --version 5.0.2`)

## Installer bauen

Im Repo liegt unter `installer/` alles Nötige, um die drei MSIs neu zu erzeugen (z.B. nach Codeänderungen):

```bash
cd Emma-Aufgabenpool
dotnet publish src/Emma.Service   -c Release -r win-x64 --self-contained true -o publish/Emma.Service
dotnet publish src/Emma.TrayApp   -c Release -r win-x64 --self-contained true -o publish/Emma.TrayApp
dotnet publish src/Emma.Viewer    -c Release -r win-x64 --self-contained true -o publish/Emma.Viewer

cd installer
powershell -File Generate-FilesWxs.ps1 -PublishDir ..\publish\Emma.TrayApp  -ComponentGroupId TrayAppFiles  -OutFile TrayApp.Files.wxs
powershell -File Generate-FilesWxs.ps1 -PublishDir ..\publish\Emma.Viewer   -ComponentGroupId ViewerFiles   -OutFile Viewer.Files.wxs
powershell -File Generate-FilesWxs.ps1 -PublishDir ..\publish\Emma.Service  -ComponentGroupId ServiceFiles  -OutFile Service.Files.wxs -ExcludeFileNames "Emma.Service.exe"

wix build TrayApp.wxs TrayApp.Files.wxs -arch x64 -o TrayApp.msi
wix build Viewer.wxs Viewer.Files.wxs   -arch x64 -o Viewer.msi
wix build Service.wxs Service.Files.wxs -arch x64 -o Service.msi
```

Die Builds sind **self-contained** (.NET-Runtime eingebettet, ~45–65 MB je MSI) – auf den Zielrechnern muss nichts vorinstalliert sein. `Generate-FilesWxs.ps1` erfasst automatisch alle Dateien im jeweiligen Publish-Ordner (Ersatz für das in WiX v5 entfallene `heat.exe`); bei Versions-/Dateiänderungen also immer zuerst neu publishen, dann die `.Files.wxs` neu generieren, dann bauen.

> Bei `wix msi validate <datei>.msi` sollte nur eine harmlose ICE60-Warnung zu `e_sqlite3.dll` übrig bleiben (fehlende Sprachangabe bei einer nativen DLL) – keine Fehler.

## Emma.Service auf dem EMMA-Rechner aufsetzen

1. `installer/Service.msi` auf den EMMA-Rechner kopieren und ausführen (Admin-Rechte nötig, UAC-Bestätigung erscheint).
   - Installiert die Programmdateien nach `C:\Program Files\EMMA Aufgabenpool Service\`
   - Registriert **automatisch einen Windows-Dienst** namens `EmmaAufgabenpoolService` (Start: automatisch, Konto: LocalSystem) und startet ihn direkt nach der Installation.
   - Erzeugt beim ersten Start automatisch unter `C:\ProgramData\EmmaAufgabenpool\Service\`:
     - `emma-service-config.json` (API-Key, Zertifikats-Passwort, Timeout-Minuten)
     - `emma-service-zertifikat.pfx` / `.cer` (selbstsigniertes TLS-Zertifikat)
     - `fuer-clients.json` (fertige Vorlage für die Client-Konfiguration)
     - `emma-aufgabenpool.db` (SQLite-Datenbank, wird per EF-Core-Migration angelegt)

   > Diese Dateien liegen bewusst **nicht** neben der .exe in `Program Files`: Windows Installer räumt beim Upgrade eines MSI-Pakets unter Umständen auch nicht-versionierte Dateien im Installationsordner mit auf, was Konfiguration/Datenbank bei jedem Update gelöscht hätte. `ProgramData` wird von MSI nie angefasst. Beim ersten Start nach einem Update wird eine ältere Version dieser Dateien (falls doch noch neben der .exe vorhanden) automatisch einmalig übernommen.
2. **Windows-Firewall freigeben** (einmalig, muss vom Betreiber selbst ausgeführt werden – macht der Installer bewusst nicht automatisch):
   ```powershell
   New-NetFirewallRule -DisplayName "EMMA Aufgabenpool Service" -Direction Inbound -Protocol TCP -LocalPort 5271 -Action Allow
   ```
3. Dienststatus prüfen: `Get-Service EmmaAufgabenpoolService` bzw. über `services.msc`.

Alternativ (z.B. für schnelle lokale Tests ohne Installation): `publish/Emma.Service` einfach kopieren und `Emma.Service.exe` direkt starten – läuft dann als Konsolen-App statt als Dienst.

## Neuen Client (Arbeitsplatz) anbinden

1. `installer/TrayApp.msi` auf dem Arbeitsplatz ausführen (Admin-Rechte für die Installation nötig, die App selbst läuft danach als normaler Benutzer). Installiert die Programmdateien nach `C:\Program Files\EMMA Aufgabenpool TrayApp\` und legt eine Startmenü-Verknüpfung an.
2. Inhalt von `fuer-clients.json` (liegt auf dem EMMA-Rechner unter `C:\ProgramData\EmmaAufgabenpool\Service\`) 1:1 in eine `emma-config.json` unter `C:\ProgramData\EmmaAufgabenpool\Emma.TrayApp\` auf dem Arbeitsplatz eintragen (Ordner ggf. erst durch einmaliges Starten der TrayApp anlegen lassen, dann die Platzhalterdatei dort überschreiben).
3. `Emma.TrayApp.exe` starten, Tray-Icon sollte erscheinen. Über "Prozess auswählen..." testen, ob Prozesse geladen werden – falls nicht, siehe [Fehlersuche](#fehlersuche).
4. Optional: im Tray-Menü "Beim Anmelden starten" aktivieren.

Der Emma.Viewer wird nur auf dem EMMA-Rechner selbst benötigt (dort, wo EMMA den Bildschirm beobachtet) – gleiches Vorgehen mit `installer/Viewer.msi`, Config-Datei unter `C:\ProgramData\EmmaAufgabenpool\Emma.Viewer\emma-config.json`.

## Laufende Wartung

### API-Key / Zertifikat rotieren

Es gibt aktuell keinen eingebauten Rotations-Mechanismus. Vorgehen bei Bedarf:

1. Service stoppen.
2. Unter `C:\ProgramData\EmmaAufgabenpool\Service\`: `emma-service-config.json`, `emma-service-zertifikat.pfx`, `emma-service-zertifikat.cer`, `fuer-clients.json` löschen.
3. Service neu starten – erzeugt alles neu, inkl. neuem Thumbprint.
4. **Neue** `fuer-clients.json` an alle Clients verteilen (alte Werte funktionieren danach nicht mehr, Verbindungen schlagen mit Zertifikatsfehler fehl).

### Timeout für "hängende" Aufgaben ändern

In `C:\ProgramData\EmmaAufgabenpool\Service\emma-service-config.json` das Feld `TimeoutMinuten` anpassen (Standard: 30), danach Service neu starten.

### Neuen Prozess hinzufügen

Aktuell gibt es keine Admin-Oberfläche dafür – Prozesse werden beim ersten Start über `SeedData.cs` (`src/Emma.Service/Data/SeedData.cs`) angelegt. Um einen weiteren Prozess zu ergänzen:

1. Neuen `Prozess`-Eintrag in `SeedData.cs` hinzufügen (Name, Beschreibung, ggf. `BenoetigtParameter`/`ParameterBezeichnung`).
2. Neu bauen und deployen. Bereits existierende Prozesse in der DB werden **nicht** erneut geseedet (`SeedProzesse` prüft `if (db.Prozesse.Any()) return;`) – ein neuer Prozess muss stattdessen direkt per SQL oder Migration in die laufende DB eingefügt werden, wenn der Service schon produktiv lief. Für eine komfortablere Lösung wäre ein Admin-Endpunkt/UI ein sinnvoller nächster Schritt.

### Datenbank sichern

Einfach `emma-aufgabenpool.db` (+ `-shm`/`-wal`-Dateien, falls vorhanden) unter `C:\ProgramData\EmmaAufgabenpool\Service\` regelmäßig sichern. Für ein konsistentes Backup den Service kurz stoppen oder ein SQLite-Online-Backup-Tool verwenden.

### Schema-Änderungen (für Entwickler)

Nach Modelländerungen in `Emma.Shared/Models`:

```bash
cd Emma-Aufgabenpool/src/Emma.Service
dotnet ef migrations add <AussagekräftigerName> --output-dir Data/Migrations
```

Der Service wendet neue Migrationen beim Start automatisch an (`db.Database.Migrate()` in `Program.cs`). Kein `EnsureCreated()` mehr verwenden – das würde bestehende Daten gefährden.

> **Achtung:** `dotnet ef migrations add/remove` führt beim Scaffolding intern denselben Startcode aus wie der echte Service (inkl. `Migrate()` + Seed), da `Program.cs` als Minimal-Hosting-Datei geschrieben ist. Bei Problemen mit "table already exists" beim nächsten echten Start: die lokale `emma-aufgabenpool.db*` im Ausgabeverzeichnis löschen und den Service neu starten.

## Fehlersuche

| Symptom | Wahrscheinliche Ursache | Lösung |
|---|---|---|
| TrayApp zeigt "Prozesse konnten nicht geladen werden" | Service nicht erreichbar, falsche `ServiceBaseUrl` | Erreichbarkeit prüfen (`Test-NetConnection <host> -Port 5271`), `emma-config.json` kontrollieren |
| Zertifikatsfehler / Verbindung wird abgelehnt | `ZertifikatThumbprint` in `emma-config.json` stimmt nicht mit dem aktuellen Server-Zertifikat überein (z.B. nach Rotation) | Aktuellen Thumbprint aus `fuer-clients.json` auf dem Server neu übernehmen |
| 401 Unauthorized | `ApiKey` in `emma-config.json` falsch/veraltet | Aktuellen Key aus `fuer-clients.json` übernehmen |
| Aufgabe bleibt ewig "Neu" | EMMA läuft nicht / Viewer-Fenster nicht offen auf dem EMMA-Rechner | Emma.Viewer auf dem EMMA-Rechner prüfen; nach `TimeoutMinuten` wird die Aufgabe automatisch als fehlgeschlagen markiert |
| Wiederkehrender Plan feuert nicht | Server-Uhrzeit falsch, Plan inaktiv, `WiederkehrendePlaeneService` nicht gelaufen (Service down) | Systemzeit auf EMMA-Rechner prüfen, Plan-Liste im TrayApp-Fenster "Wiederkehrende Pläne" kontrollieren |
| Service startet nicht, Fehler zu Firewall/Port | Port 5271 durch anderen Prozess belegt oder Firewall blockiert | `netstat -ano \| findstr 5271`, Firewall-Regel prüfen |

Logs des Service laufen bei Konsolen-Betrieb direkt in die Konsole; bei Betrieb als Windows-Dienst empfiehlt sich, Ausgaben in eine Log-Datei umzuleiten (z.B. via NSSM-Konfiguration).

## Offene Punkte / bekannte Grenzen

- Kein Admin-UI zum Anlegen/Bearbeiten von Prozessen (nur über `SeedData.cs` + Redeploy).
- Kein automatischer Rotations-Mechanismus für API-Key/Zertifikat.
- Keine Rechteverwaltung pro Prozess (jeder mit gültigem API-Key kann jeden Prozess anstoßen) – bewusst nicht umgesetzt.
- Bei mehreren parallelen EMMA-Instanzen fehlt ein Locking-Mechanismus, um doppelte Bearbeitung derselben Aufgabe zu verhindern.
