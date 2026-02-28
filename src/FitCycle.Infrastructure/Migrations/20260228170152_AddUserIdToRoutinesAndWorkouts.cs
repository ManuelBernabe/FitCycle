using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToRoutinesAndWorkouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DayMuscleGroups_Day_MuscleGroupId",
                table: "DayMuscleGroups");

            migrationBuilder.DropIndex(
                name: "IX_DayExercises_Day_ExerciseId",
                table: "DayExercises");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "WorkoutSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "DayMuscleGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "DayExercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DayMuscleGroups_UserId_Day_MuscleGroupId",
                table: "DayMuscleGroups",
                columns: new[] { "UserId", "Day", "MuscleGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DayExercises_UserId_Day_ExerciseId",
                table: "DayExercises",
                columns: new[] { "UserId", "Day", "ExerciseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DayMuscleGroups_UserId_Day_MuscleGroupId",
                table: "DayMuscleGroups");

            migrationBuilder.DropIndex(
                name: "IX_DayExercises_UserId_Day_ExerciseId",
                table: "DayExercises");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DayMuscleGroups");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DayExercises");

            migrationBuilder.CreateIndex(
                name: "IX_DayMuscleGroups_Day_MuscleGroupId",
                table: "DayMuscleGroups",
                columns: new[] { "Day", "MuscleGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DayExercises_Day_ExerciseId",
                table: "DayExercises",
                columns: new[] { "Day", "ExerciseId" });
        }
    }
}
