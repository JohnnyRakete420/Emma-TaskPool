using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Service.Data.Migrations
{
    /// <inheritdoc />
    public partial class WiederkehrendePlanSpaltenBereinigung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LetzteAusfuehrung",
                table: "WiederkehrendePlaene");

            migrationBuilder.DropColumn(
                name: "Wochentag",
                table: "WiederkehrendePlaene");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "LetzteAusfuehrung",
                table: "WiederkehrendePlaene",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Wochentag",
                table: "WiederkehrendePlaene",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
