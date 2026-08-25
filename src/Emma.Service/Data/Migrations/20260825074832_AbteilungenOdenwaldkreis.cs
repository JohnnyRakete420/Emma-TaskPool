using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Service.Data.Migrations
{
    /// <inheritdoc />
    public partial class AbteilungenOdenwaldkreis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ersetzt die generische Platzhalter-Abteilungsliste (aus der vorherigen Migration
            // ParameterFelderTypisierung) durch das tatsächliche Organigramm des Odenwaldkreises.
            // Alle anderen Felder von "Benutzer anlegen" bleiben unverändert.
            migrationBuilder.Sql(
                """
                UPDATE Prozesse
                SET ParameterFelderJson = '[{"Bezeichnung":"Vorname","Typ":0,"Optionen":null},{"Bezeichnung":"Nachname","Typ":0,"Optionen":null},{"Bezeichnung":"Führungskraft","Typ":0,"Optionen":null},{"Bezeichnung":"Eintrittsdatum","Typ":0,"Optionen":null},{"Bezeichnung":"Abteilungen","Typ":1,"Optionen":["Bau- und Immobilienmanagement Odenwaldkreis","Chancengleichheit, Integration und Diversität","Erster Kreisbeigeordneter","Hauptamtlicher Kreisbeigeordneter","I Bürgerservice","I Gremienbüro","I Hauptabteilung Zentrale Verwaltungsaufgaben","I Organisation","I.20 Personalamt","I.30 Ehrenamt, Kultur","I.40 Dorf- und Regionalentwicklung","I.60 Finanz- und Rechnungswesen","I.80 E-Government, Digitalisierung und IT","I.90 Archiv und Schriftgutverwaltung","II Hauptabteilung Arbeit und Soziale Sicherung","II.10 Kommunales Job-Center","II.20 Soziale Sicherung","II.30 Kommunales Service-Center","III.10 Schulverwaltung","III.30 Jugendamt","III.40 Kinder- und Jugendförderung","III.50 Erziehungsberatungsstelle","III.60 Volkshochschule Odenwaldkreis","IV Hauptabteilung Bauwesen","IV.10 Allgemeine Bauverwaltung","IV.10 Wohngeldbehörde","IV.20 Bauaufsicht, Bauleit- und Regionalplanung, Denkmalschutz","Leitstelle","Medienzentrum Odenwaldkreis","Personalrat","Revisionsamt","Stabsstelle Erster Kreisbeigeordneter","Stabsstelle Landrat","V Hauptabteilung Landesaufgaben, Umwelt und Verkehr, Rechtsamt","V.10 Rechtsamt","V.20 Kommunalaufsicht","V.30 Öffentliche Sicherheit und Ordnung","V.35 Ausländerbehörde","V.50 Umwelt, Naturschutz und Landschaftspflege","V.60 Verkehrswesen","V.70 Brand- und Katastrophenschutz, Rettungsdienst, Maklerwesen","V.80 Landwirtschaft und landwirtschaftliche Förderung","VI Hauptabteilung Gesundheit, Veterinärwesen + Verbraucherschutz","VI.10 Gesundheitsamt","VI.20 Veterinärwesen und Verbraucherschutz"]},{"Bezeichnung":"Benötigte Software","Typ":2,"Optionen":["AD","Exchange","Dokuneo","Intranet","Teams","Proofpoint","Drucker","Telefonbuch","Idento21","Prosoz 14+","Telefonanlage"]},{"Bezeichnung":"Benötigte Laufwerke","Typ":0,"Optionen":null},{"Bezeichnung":"Benötigte Postfächer","Typ":0,"Optionen":null}]'
                WHERE Name = 'Benutzer anlegen';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
