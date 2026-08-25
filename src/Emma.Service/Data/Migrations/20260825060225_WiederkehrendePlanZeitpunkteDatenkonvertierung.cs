using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Service.Data.Migrations
{
    /// <inheritdoc />
    public partial class WiederkehrendePlanZeitpunkteDatenkonvertierung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alt-Werte (ein Wochentag + eine Uhrzeit pro Plan, aus der vorherigen Version) in das
            // neue ZeitpunkteJson-Format (Liste von {Wochentag, Uhrzeit, LetzteAusfuehrung}) überführen,
            // solange Wochentag/LetzteAusfuehrung noch vorhanden sind (werden erst in
            // WiederkehrendePlanSpaltenBereinigung entfernt). In einer eigenen Migration, damit dies erst
            // NACH Abschluss des SQLite-Tabellen-Rebuilds aus der vorherigen Migration läuft.
            migrationBuilder.Sql(
                """
                UPDATE WiederkehrendePlaene
                SET ZeitpunkteJson =
                    '[{"Wochentag":' || Wochentag ||
                    ',"Uhrzeit":"' || substr(ZeitpunkteJson, 1, 5) || ':00"' ||
                    ',"LetzteAusfuehrung":' ||
                    (CASE WHEN LetzteAusfuehrung IS NULL THEN 'null' ELSE '"' || LetzteAusfuehrung || '"' END) ||
                    '}]'
                WHERE ZeitpunkteJson IS NOT NULL
                  AND ZeitpunkteJson <> ''
                  AND ZeitpunkteJson NOT LIKE '[%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
