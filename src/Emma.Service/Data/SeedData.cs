using Emma.Shared;
using Emma.Shared.Models;

namespace Emma.Service.Data;

public static class SeedData
{
    /// <summary>
    /// Standard-Prozesse, die es geben soll. Wird bei jedem Start geprüft - ergänzt nur die
    /// Prozesse, die (nach Name) noch fehlen, statt bei nicht-leerer Tabelle komplett zu überspringen.
    /// So tauchen neu hinzugefügte Standardprozesse auch bei einer bestehenden Installation
    /// nach einem Update automatisch auf, ohne vorhandene Daten anzufassen.
    /// </summary>
    /// <summary>Abteilungen des Odenwaldkreises für das "Abteilungen"-Auswahlfeld bei "Benutzer anlegen".</summary>
    private static readonly List<string> Abteilungen =
    [
        "Bau- und Immobilienmanagement Odenwaldkreis",
        "Chancengleichheit, Integration und Diversität",
        "Erster Kreisbeigeordneter",
        "Hauptamtlicher Kreisbeigeordneter",
        "I Bürgerservice",
        "I Gremienbüro",
        "I Hauptabteilung Zentrale Verwaltungsaufgaben",
        "I Organisation",
        "I.20 Personalamt",
        "I.30 Ehrenamt, Kultur",
        "I.40 Dorf- und Regionalentwicklung",
        "I.60 Finanz- und Rechnungswesen",
        "I.80 E-Government, Digitalisierung und IT",
        "I.90 Archiv und Schriftgutverwaltung",
        "II Hauptabteilung Arbeit und Soziale Sicherung",
        "II.10 Kommunales Job-Center",
        "II.20 Soziale Sicherung",
        "II.30 Kommunales Service-Center",
        "III.10 Schulverwaltung",
        "III.30 Jugendamt",
        "III.40 Kinder- und Jugendförderung",
        "III.50 Erziehungsberatungsstelle",
        "III.60 Volkshochschule Odenwaldkreis",
        "IV Hauptabteilung Bauwesen",
        "IV.10 Allgemeine Bauverwaltung",
        "IV.10 Wohngeldbehörde",
        "IV.20 Bauaufsicht, Bauleit- und Regionalplanung, Denkmalschutz",
        "Leitstelle",
        "Medienzentrum Odenwaldkreis",
        "Personalrat",
        "Revisionsamt",
        "Stabsstelle Erster Kreisbeigeordneter",
        "Stabsstelle Landrat",
        "V Hauptabteilung Landesaufgaben, Umwelt und Verkehr, Rechtsamt",
        "V.10 Rechtsamt",
        "V.20 Kommunalaufsicht",
        "V.30 Öffentliche Sicherheit und Ordnung",
        "V.35 Ausländerbehörde",
        "V.50 Umwelt, Naturschutz und Landschaftspflege",
        "V.60 Verkehrswesen",
        "V.70 Brand- und Katastrophenschutz, Rettungsdienst, Maklerwesen",
        "V.80 Landwirtschaft und landwirtschaftliche Förderung",
        "VI Hauptabteilung Gesundheit, Veterinärwesen + Verbraucherschutz",
        "VI.10 Gesundheitsamt",
        "VI.20 Veterinärwesen und Verbraucherschutz"
    ];

    private static readonly List<string> BenoetigteSoftwareOptionen =
    [
        "AD", "Exchange", "Dokuneo", "Intranet", "Teams", "Proofpoint", "Drucker",
        "Telefonbuch", "Idento21", "Prosoz 14+", "Telefonanlage"
    ];

    private static List<Prozess> ErwarteteProzesse() =>
    [
        new Prozess { Name = "Kassenautomat", Beschreibung = "Tagesabschluss am Kassenautomaten durchführen" },
        new Prozess
        {
            Name = "Posteingang Veterinäramt",
            Beschreibung = "Posteingang des Veterinäramts bearbeiten"
        },
        new Prozess { Name = "Google Whats New", Beschreibung = "Google Whats-New-Feed prüfen" },
        new Prozess
        {
            Name = "Benutzer anlegen",
            Beschreibung = "Neuen Benutzer-Account anlegen",
            ParameterFelderJson = ParameterJsonHelper.SerializeFelder(
            [
                new ParameterFeldDefinition("Vorname"),
                new ParameterFeldDefinition("Nachname"),
                new ParameterFeldDefinition("Führungskraft"),
                new ParameterFeldDefinition("Eintrittsdatum"),
                new ParameterFeldDefinition("Abteilungen", ParameterFeldTyp.Auswahl, Abteilungen),
                new ParameterFeldDefinition("Benötigte Software", ParameterFeldTyp.Mehrfachauswahl, BenoetigteSoftwareOptionen),
                new ParameterFeldDefinition("Benötigte Laufwerke"),
                new ParameterFeldDefinition("Benötigte Postfächer")
            ])
        }
    ];

    public static void SeedProzesse(EmmaDbContext db)
    {
        var vorhandeneNamen = db.Prozesse.Select(p => p.Name).ToHashSet();

        var fehlende = ErwarteteProzesse()
            .Where(p => !vorhandeneNamen.Contains(p.Name))
            .ToList();

        if (fehlende.Count == 0)
            return;

        db.Prozesse.AddRange(fehlende);
        db.SaveChanges();
    }
}
