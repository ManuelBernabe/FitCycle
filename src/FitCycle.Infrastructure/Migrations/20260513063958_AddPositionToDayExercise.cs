using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionToDayExercise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "DayExercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Seed existing rows with a stable Position based on their insertion order (Id).
            // Each (UserId, Day) group gets positions 0,1,2,... ordered by Id ascending.
            migrationBuilder.Sql(@"
                UPDATE DayExercises
                SET Position = (
                    SELECT COUNT(*) FROM DayExercises AS d2
                    WHERE d2.UserId = DayExercises.UserId
                      AND d2.Day = DayExercises.Day
                      AND d2.Id < DayExercises.Id
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "DayExercises");
        }
    }
}
