using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Service.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParameterFelderTypisierung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Format-Konvertierung: Prozesse.ParameterFelderJson war bisher ein Array von reinen
            // Feldnamen (["Vorname","Nachname",...]); ab jetzt ein Array von Feld-Definitionen mit
            // Eingabeart ({"Bezeichnung":"Vorname","Typ":0,...}). Betrifft generell alle Prozesse mit
            // Alt-Format-Daten, nicht nur die beiden unten inhaltlich angepassten.
            migrationBuilder.Sql(
                """
                UPDATE Prozesse
                SET ParameterFelderJson = (
                    SELECT json_group_array(json_object('Bezeichnung', value, 'Typ', 0))
                    FROM json_each(ParameterFelderJson)
                )
                WHERE ParameterFelderJson IS NOT NULL
                  AND ParameterFelderJson <> ''
                  AND ParameterFelderJson NOT LIKE '%"Bezeichnung"%';
                """);

            // Fachliche Änderung: "Posteingang Veterinäramt" braucht kein Datumsfeld mehr.
            migrationBuilder.Sql(
                """
                UPDATE Prozesse
                SET ParameterFelderJson = NULL
                WHERE Name = 'Posteingang Veterinäramt';
                """);

            // Fachliche Änderung: "Benutzer anlegen" bekommt den vollständigen neuen Feldsatz
            // (Vorname, Nachname, Führungskraft, Eintrittsdatum, Abteilungen-Dropdown,
            // Software-Häkchen, Laufwerke, Postfächer) statt der bisherigen 5 Freitextfelder.
            migrationBuilder.Sql(
                """
                UPDATE Prozesse
                SET ParameterFelderJson = '[{"Bezeichnung":"Vorname","Typ":0,"Optionen":null},{"Bezeichnung":"Nachname","Typ":0,"Optionen":null},{"Bezeichnung":"Führungskraft","Typ":0,"Optionen":null},{"Bezeichnung":"Eintrittsdatum","Typ":0,"Optionen":null},{"Bezeichnung":"Abteilungen","Typ":1,"Optionen":["Landratsamt / Zentrale Steuerung","Personalamt","Kämmerei / Finanzen","Ordnungsamt","Jugendamt","Sozialamt","Gesundheitsamt","Veterinäramt","Bauamt","Straßenverkehrsamt","Kreisarchiv","IT / Digitalisierung","Schulamt"]},{"Bezeichnung":"Benötigte Software","Typ":2,"Optionen":["AD","Exchange","Dokuneo","Intranet","Teams","Proofpoint","Drucker","Telefonbuch","Idento21","Prosoz 14+","Telefonanlage"]},{"Bezeichnung":"Benötigte Laufwerke","Typ":0,"Optionen":null},{"Bezeichnung":"Benötigte Postfächer","Typ":0,"Optionen":null}]'
                WHERE Name = 'Benutzer anlegen';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
