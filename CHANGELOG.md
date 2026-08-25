# Änderungsprotokoll

Versionsverlauf der drei Pakete (`installer/Service.msi`, `installer/TrayApp.msi`, `installer/Viewer.msi`). Jedes Paket hat seine eigene Versionsnummer und wird unabhängig aktualisiert - siehe [BETRIEB.md](BETRIEB.md) für den Installations-/Update-Ablauf.

> Die Einträge ab den unten markierten "Stand zu Beginn dieser Session"-Punkten sind versionsgenau protokolliert. Alles davor (erste Grundentwicklung: Architektur, HTTPS/Zertifikats-Absicherung, MSI-Installer, erste Designs) ist nur gesammelt zusammengefasst, weil dafür keine versionsweise Aufschlüsselung mehr vorliegt.

## Emma.Service (`Service.msi`)

### 1.2.4
- Echte Abteilungsliste des Odenwaldkreises (45 Einträge, Hauptabteilungen I-VI + Stabsstellen) für das Formularfeld "Abteilungen" bei "Benutzer anlegen" hinterlegt (ersetzt die vorherige, generische Platzhalterliste)

### 1.2.3
- Formularfelder pro Prozess können jetzt **typisiert** sein: Freitext, Dropdown (Auswahl) oder Häkchen (Mehrfachauswahl) - vorher war jedes Feld reiner Freitext
- Serverseitige Validierung prüft bei Auswahl-/Häkchenfeldern zusätzlich, ob die übermittelten Werte zu den erlaubten Optionen gehören
- Prozess "Posteingang Veterinäramt": Datumsfeld entfernt (wird nicht mehr benötigt)
- Prozess "Benutzer anlegen": neuer Feldsatz - Vorname, Nachname, Führungskraft, Eintrittsdatum, Abteilungen (Dropdown), Benötigte Software (Häkchen: AD, Exchange, Dokuneo, Intranet, Teams, Proofpoint, Drucker, Telefonbuch, Idento21, Prosoz 14+, Telefonanlage), Benötigte Laufwerke, Benötigte Postfächer

### 1.2.2
- Wiederkehrende Pläne komplett neu: ein Plan kann jetzt mehrere Wochentage abdecken und pro Wochentag mehrere/unterschiedliche Uhrzeiten haben (vorher: genau ein Wochentag + eine Uhrzeit pro Plan)
- Neuer API-Endpunkt zum **Bearbeiten** eines bestehenden Plans (vorher nur Anlegen und Löschen möglich)

### 1.2.1 und früher — *Stand zu Beginn dieser Session*
- Grundfunktionen: Aufgaben anlegen/abrufen/als erledigt bzw. fehlgeschlagen markieren, wiederkehrende Pläne (altes Ein-Tag-Format), automatischer Zeitüberschreitungs-Wächter für hängengebliebene Aufgaben
- HTTPS mit Zertifikats-Pinning (kein Eingriff in den Windows-Zertifikatsspeicher) + API-Key-Absicherung (`X-Api-Key`-Header)
- Konfiguration, Zertifikat und Datenbank liegen in `ProgramData` statt neben der .exe, damit MSI-Updates sie nicht mehr löschen
- Mehrfeld-Parameter pro Prozess eingeführt (JSON-basiert, beliebig viele benannte Formularfelder statt nur einem)
- Windows-Dienst-Registrierung (`EmmaAufgabenpoolService`, läuft als LocalSystem)

## Emma.TrayApp (`TrayApp.msi`)

### 1.7.0
- Neues Bestätigungsfenster nach "An EMMA übergeben": zeigt in einem kleinen Popup an, ob die Aufgabe erfolgreich in den Aufgabenpool aufgenommen wurde oder ob das fehlgeschlagen ist (mit Fehlermeldung) - stellt klar, dass das nur die Übergabe bestätigt, nicht dass EMMA die Aufgabe bereits erledigt hat

### 1.6.0
- Neues **Einstellungen**-Fenster (Rechtsklick auf das Tray-Symbol → "Einstellungen..."), lokal pro Rechner:
  - "Beim Anmelden starten" (vorher direkt im Kontextmenü, jetzt hierher verschoben)
  - Benachrichtigungen an/aus (die Windows-Sprechblasen bei Erfolg/Fehlschlag lassen sich jetzt abschalten)
  - Anzeige der installierten Version

### 1.5.0
- Neue Eingabetypen im Formular: Dropdown- und Häkchen-Felder werden jetzt automatisch mit dem passenden Steuerelement angezeigt (statt für jedes Feld nur eine Textbox)

### 1.4.0
- **Wiederkehrende Pläne** komplett neu aufgebaut: "Neu"-Button statt Inline-Formular, Häkchen für mehrere Wochentage gleichzeitig, mehrere/unterschiedliche Uhrzeiten pro Tag möglich, deutsches Zeitformat ("20:00 Uhr" statt "8:00 PM"), "Bearbeiten" statt "Löschen" in der Liste, Doppelklick auf eine Zeile öffnet eine Leseansicht
- **Verlauf & Übersicht** überarbeitet: moderne, selbst gestaltete Dropdowns, kompaktere Tabelle (Prozess/Status/Parameter/Erstellt am) mit Detailfenster per Doppelklick für den Rest, schmaleres Suchfeld, 4 Statistik-Kacheln (Gesamt/Erledigt/Fehlgeschlagen/Erfolgsquote) statt Tabelle

### 1.3.0 und früher — *Stand zu Beginn dieser Session*
- Grundfunktionen: Prozess auswählen und als Aufgabe an EMMA übergeben, Verlauf ansehen, wiederkehrende Pläne verwalten (altes Ein-Tag-Format)
- Modernes Design in der Marken-Farbpalette (abgerundete Karten, Buttons, eigene ComboBox-Gestaltung)
- Mehrfeld-Formulare pro Prozess (z.B. "Benutzer anlegen" mit mehreren benannten Feldern statt einem Freitextfeld)
- Absturz-Schutz (Verzeichnis-Erstellung abgesichert, globaler Fehler-Handler)

## Emma.Viewer (`Viewer.msi`)

### 1.4.0 — *aktuellster Stand, in dieser Session nicht verändert*
- Card-basierte Ansicht der offenen Aufgaben statt Tabelle - besser lesbar für EMMA beim Screen-Scraping
- "Bearbeiten"-Workflow: Klick markiert die Aufgabe als "In Bearbeitung", öffnet eine Detailansicht mit großen, klar beschrifteten Blöcken pro Formularfeld und minimiert das Hauptfenster
