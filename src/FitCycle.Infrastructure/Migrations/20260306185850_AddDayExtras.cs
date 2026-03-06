using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDayExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DayExtras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    CardioType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CardioMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AbsExercise = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AbsMinutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayExtras", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DayExtras_UserId_Day",
                table: "DayExtras",
                columns: new[] { "UserId", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DayExtras");
        }
    }
}
