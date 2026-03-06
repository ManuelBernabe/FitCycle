using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId",
                table: "WorkoutSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_UserId",
                table: "WorkoutSessions");
        }
    }
}
