using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BodyMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: true),
                    Height = table.Column<decimal>(type: "TEXT", nullable: true),
                    Chest = table.Column<decimal>(type: "TEXT", nullable: true),
                    Waist = table.Column<decimal>(type: "TEXT", nullable: true),
                    Hips = table.Column<decimal>(type: "TEXT", nullable: true),
                    BicepLeft = table.Column<decimal>(type: "TEXT", nullable: true),
                    BicepRight = table.Column<decimal>(type: "TEXT", nullable: true),
                    ThighLeft = table.Column<decimal>(type: "TEXT", nullable: true),
                    ThighRight = table.Column<decimal>(type: "TEXT", nullable: true),
                    CalfLeft = table.Column<decimal>(type: "TEXT", nullable: true),
                    CalfRight = table.Column<decimal>(type: "TEXT", nullable: true),
                    Neck = table.Column<decimal>(type: "TEXT", nullable: true),
                    BodyFat = table.Column<decimal>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyMeasurements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BodyMeasurements_UserId_MeasuredAt",
                table: "BodyMeasurements",
                columns: new[] { "UserId", "MeasuredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BodyMeasurements");
        }
    }
}
