using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAbsMinutesWithSetsReps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AbsMinutes",
                table: "DayExtras",
                newName: "AbsSets");

            migrationBuilder.AddColumn<int>(
                name: "AbsReps",
                table: "DayExtras",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsReps",
                table: "DayExtras");

            migrationBuilder.RenameColumn(
                name: "AbsSets",
                table: "DayExtras",
                newName: "AbsMinutes");
        }
    }
}
