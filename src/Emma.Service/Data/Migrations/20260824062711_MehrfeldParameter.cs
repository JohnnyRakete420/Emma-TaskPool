using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Service.Data.Migrations
{
    /// <inheritdoc />
    public partial class MehrfeldParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BenoetigtParameter",
                table: "Prozesse");

            migrationBuilder.RenameColumn(
                name: "Parameter",
                table: "WiederkehrendePlaene",
                newName: "ParameterJson");

            migrationBuilder.RenameColumn(
                name: "ParameterBezeichnung",
                table: "Prozesse",
                newName: "ParameterFelderJson");

            migrationBuilder.RenameColumn(
                name: "Parameter",
                table: "Aufgaben",
                newName: "ParameterJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParameterJson",
                table: "WiederkehrendePlaene",
                newName: "Parameter");

            migrationBuilder.RenameColumn(
                name: "ParameterFelderJson",
                table: "Prozesse",
                newName: "ParameterBezeichnung");

            migrationBuilder.RenameColumn(
                name: "ParameterJson",
                table: "Aufgaben",
                newName: "Parameter");

            migrationBuilder.AddColumn<bool>(
                name: "BenoetigtParameter",
                table: "Prozesse",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
