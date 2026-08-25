using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Service.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prozesse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                    BenoetigtParameter = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParameterBezeichnung = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prozesse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Aufgaben",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProzessId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErstelltVon = table.Column<string>(type: "TEXT", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AbgeschlossenAm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Parameter = table.Column<string>(type: "TEXT", nullable: true),
                    Fehlermeldung = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aufgaben", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Prozesse_ProzessId",
                        column: x => x.ProzessId,
                        principalTable: "Prozesse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WiederkehrendePlaene",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProzessId = table.Column<int>(type: "INTEGER", nullable: false),
                    Wochentag = table.Column<int>(type: "INTEGER", nullable: false),
                    Uhrzeit = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false),
                    LetzteAusfuehrung = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Parameter = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WiederkehrendePlaene", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WiederkehrendePlaene_Prozesse_ProzessId",
                        column: x => x.ProzessId,
                        principalTable: "Prozesse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_ProzessId",
                table: "Aufgaben",
                column: "ProzessId");

            migrationBuilder.CreateIndex(
                name: "IX_WiederkehrendePlaene_ProzessId",
                table: "WiederkehrendePlaene",
                column: "ProzessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Aufgaben");

            migrationBuilder.DropTable(
                name: "WiederkehrendePlaene");

            migrationBuilder.DropTable(
                name: "Prozesse");
        }
    }
}
