# Änderungsprotokoll

Versionsverlauf der drei Pakete (`installer/Service.msi`, `installer/TrayApp.msi`, `installer/Viewer.msi`). Jedes Paket hat seine eigene Versionsnummer und wird unabhängig aktualisiert - siehe [BETRIEB.md](BETRIEB.md) für den Installations-/Update-Ablauf.

> Die Einträge ab den unten markierten "Stand zu Beginn dieser Session"-Punkten sind versionsgenau protokolliert. Alles davor (erste Grundentwicklung: Architektur, HTTPS/Zertifikats-Absicherung, MSI-Installer, erste Designs) ist nur gesammelt zusammengefasst, weil dafür keine versionsweise Aufschlüsselung mehr vorliegt.

## Emma.Service (`Service.msi`)

### 1.3.0
- Neue Endpunkte zum Anlegen, Bearbeiten und Löschen von Prozessen (`POST`/`PUT`/`DELETE /api/prozesse`) - Prozesse mussten bisher fest im Code hinterlegt und neu deployed werden. Alle TrayApp-Nutzer dürfen Prozesse verwalten (kein separater Admin-Bereich)
- Validiert Name (Pflicht, eindeutig) und Formularfelder (Bezeichnung Pflicht+eindeutig, Auswahl/Mehrfachauswahl brauchen mindestens eine Option)
- Ein Prozess mit vorhandenem Aufgaben-Verlauf oder wiederkehrenden Plänen kann nicht gelöscht werden (verhindert versehentliches Löschen der Historie durch die Datenbank-Kaskade)

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

### 1.9.0
- **Komplett selbst gezeichnetes Fenster-Chrome** (WindowChrome) statt der nativen Windows-Titelleiste/des nativen Rahmens - löst den hellen Akzentrahmen aus 1.8.3 endgültig, weil es den nativen Rahmen gar nicht mehr gibt. Jedes Fenster hat jetzt eine eigene, thematisch passende Titelleiste mit Icon, Titel und eigenen Minimieren/Maximieren/Schließen-Buttons; Ziehen/Doppelklick-zum-Maximieren/Größe ändern funktioniert weiterhin wie gewohnt (übernimmt WindowChrome automatisch)
- Technische Notiz: der Fenster-Style musste **explizit** referenziert werden (`Style="{StaticResource FensterChrome}"` in jedem Fenster) - die sonst übliche implizite Zuordnung (nur `TargetType`, ohne Key) hat das Template beim Testen nicht zuverlässig angewendet

### 1.8.3
- Echter Lesbarkeits-Bug behoben: Tabellenzellen (Verlauf, Prozesse verwalten, Wiederkehrende Pläne) hatten nie eine explizite Textfarbe gesetzt - nur beim Anklicken/Auswählen einer Zeile griff ein Trigger, der die Farbe auf Weiß setzte. Im hellen Design fiel das nie auf (WPFs Standard-Textfarbe ist zufällig dunkel), im Dunklen Design war der Text dadurch bis zum Anklicken quasi unsichtbar. Jetzt hat jede Zelle von Anfang an die richtige Farbe
- Der Windows-11-Akzentrahmen um die Fenster ist bei manchen Nutzern weiterhin hell geblieben - liegt vermutlich an fehlender/übersteuerter Unterstützung des `DWMWA_BORDER_COLOR`-Attributs auf dem jeweiligen System, nicht behebbar ohne auf komplett selbst gezeichnetes Fenster-Chrome umzusteigen

### 1.8.2
- Dunkles Design weiter nachgebessert:
  - Text (v.a. Beschriftungen/Nebentext) heller gestellt - Haupttext jetzt reinweiß statt leicht getönt
  - Der dünne Windows-11-Akzentrahmen um jedes Fenster blieb hell, obwohl Titelleiste und Inhalt schon dunkel waren - wird jetzt per DWM mit eingefärbt (nur Windows 11 22H2+, auf älteren Systemen ohne Effekt)

### 1.8.1
- Dunkles Design nachgebessert (Rückmeldung: "sieht nicht fertig aus"):
  - Die Datumsfelder ("Von"/"Bis" im Verlauf) waren komplett unthemed und standen als helle Kästen im dunklen Fenster - jetzt an die Palette angepasst (das kleine Kalender-Symbol bleibt aus Aufwandsgründen im Windows-Standard)
  - Abwechselnde Tabellenzeilen waren im Dunklen kaum zu unterscheiden - Kontrast erhöht
  - Die native Fenster-Titelleiste blieb bisher hell, obwohl der Inhalt dunkel wurde - wird jetzt bei aktivem Dunklen Design ebenfalls eingefärbt

### 1.8.0
- **Prozesse verwalten**: neues Fenster (Tray-Menü → "Prozesse verwalten...") zum Anlegen/Bearbeiten/Löschen von Prozessen inkl. Formularfeldern (Text/Auswahl/Mehrfachauswahl mit Optionen) - vorher nur per Code-Änderung möglich
- **Dunkles Design**: Umschalter in den Einstellungen, wirkt nach einem Neustart der Anwendung. Komplette Farbpalette für dunklen Hintergrund abgestimmt
- **Server-Verbindung einrichten**: Server-Adresse, API-Key und Zertifikats-Thumbprint lassen sich jetzt direkt im Einstellungen-Fenster eintragen statt die `emma-config.json` von Hand zu bearbeiten
- **Verbindung testen**: Button in den Einstellungen, der sofort prüft, ob der Service unter den eingetragenen Daten erreichbar ist
- **Tray-Symbol zeigt Verbindungsstatus**: grüner/roter Punkt je nachdem, ob der Service gerade erreichbar ist - ein Ausfall fällt jetzt sofort auf, statt erst beim Öffnen eines Fensters
- **Zuletzt verwendete Prozesse zuerst**: die Prozessliste beim Anlegen einer Aufgabe sortiert sich automatisch danach, was zuletzt benutzt wurde

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
