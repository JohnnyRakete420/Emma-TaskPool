using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Service.Data.Migrations
{
    /// <inheritdoc />
    public partial class WiederkehrendePlanZeitpunkte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nur Umbenennen - Wochentag/LetzteAusfuehrung bleiben vorerst bestehen, damit die
            // Datenkonvertierungs-Migration (WiederkehrendePlanZeitpunkteDatenkonvertierung) daraus
            // noch das neue ZeitpunkteJson bauen kann, bevor sie in
            // WiederkehrendePlanSpaltenBereinigung entfernt werden.
            migrationBuilder.RenameColumn(
                name: "Uhrzeit",
                table: "WiederkehrendePlaene",
                newName: "ZeitpunkteJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ZeitpunkteJson",
                table: "WiederkehrendePlaene",
                newName: "Uhrzeit");
        }
    }
}
