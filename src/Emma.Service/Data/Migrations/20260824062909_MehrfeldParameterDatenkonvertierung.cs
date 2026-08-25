using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Service.Data.Migrations
{
    /// <inheritdoc />
    public partial class MehrfeldParameterDatenkonvertierung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alt-Werte aus der vorherigen Ein-Parameter-Version (einfacher Text) in das
            // neue JSON-Format überführen, statt sie als kaputtes JSON stehen zu lassen.
            // In einer eigenen Migration, damit dies erst NACH Abschluss des SQLite-
            // Tabellen-Rebuilds aus der vorherigen Migration läuft.
            migrationBuilder.Sql(
                """
                UPDATE Prozesse
                SET ParameterFelderJson = '[' || json_quote(ParameterFelderJson) || ']'
                WHERE ParameterFelderJson IS NOT NULL
                  AND ParameterFelderJson <> ''
                  AND ParameterFelderJson NOT LIKE '[%';
                """);

            migrationBuilder.Sql(
                """
                UPDATE Aufgaben
                SET ParameterJson = '[{"Bezeichnung":"Parameter","Wert":' || json_quote(ParameterJson) || '}]'
                WHERE ParameterJson IS NOT NULL
                  AND ParameterJson <> ''
                  AND ParameterJson NOT LIKE '[%';
                """);

            migrationBuilder.Sql(
                """
                UPDATE WiederkehrendePlaene
                SET ParameterJson = '[{"Bezeichnung":"Parameter","Wert":' || json_quote(ParameterJson) || '}]'
                WHERE ParameterJson IS NOT NULL
                  AND ParameterJson <> ''
                  AND ParameterJson NOT LIKE '[%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
