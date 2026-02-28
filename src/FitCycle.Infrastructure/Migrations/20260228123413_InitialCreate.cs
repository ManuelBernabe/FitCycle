using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FitCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MuscleGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuscleGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DayMuscleGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    MuscleGroupId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayMuscleGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DayMuscleGroups_MuscleGroups_MuscleGroupId",
                        column: x => x.MuscleGroupId,
                        principalTable: "MuscleGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MuscleGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercises_MuscleGroups_MuscleGroupId",
                        column: x => x.MuscleGroupId,
                        principalTable: "MuscleGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutExerciseLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkoutSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sets = table.Column<int>(type: "INTEGER", nullable: false),
                    Reps = table.Column<int>(type: "INTEGER", nullable: false),
                    MuscleGroupName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutExerciseLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutExerciseLogs_WorkoutSessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DayExercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sets = table.Column<int>(type: "INTEGER", nullable: false),
                    Reps = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DayExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MuscleGroups",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Pecho" },
                    { 2, "Espalda" },
                    { 3, "Hombros" },
                    { 4, "Bíceps" },
                    { 5, "Tríceps" },
                    { 6, "Piernas" },
                    { 7, "Abdominales" },
                    { 8, "Glúteos" }
                });

            migrationBuilder.InsertData(
                table: "Exercises",
                columns: new[] { "Id", "ImageUrl", "MuscleGroupId", "Name" },
                values: new object[,]
                {
                    { 1, "https://wger.de/media/exercise-images/192/Bench-press-1.png", 1, "Press banca" },
                    { 2, "https://wger.de/media/exercise-images/41/Incline-bench-press-1.png", 1, "Press inclinado" },
                    { 3, "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png", 1, "Aperturas" },
                    { 4, "https://wger.de/media/exercise-images/83/Bench-dips-1.png", 1, "Fondos" },
                    { 5, "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg", 2, "Dominadas" },
                    { 6, "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png", 2, "Remo con barra" },
                    { 7, "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp", 2, "Jalón al pecho" },
                    { 8, "https://wger.de/media/exercise-images/81/a751a438-ae2d-4751-8d61-cef0e9292174.png", 2, "Remo con mancuerna" },
                    { 9, "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png", 3, "Press militar" },
                    { 10, "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png", 3, "Elevaciones laterales" },
                    { 11, "https://wger.de/media/exercise-images/256/b7def5bc-2352-499b-b9e5-fff741003831.png", 3, "Elevaciones frontales" },
                    { 12, "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png", 3, "Pájaros" },
                    { 13, "https://wger.de/media/exercise-images/74/Bicep-curls-1.png", 4, "Curl con barra" },
                    { 14, "https://wger.de/media/exercise-images/81/Biceps-curl-1.png", 4, "Curl con mancuernas" },
                    { 15, "https://wger.de/media/exercise-images/86/Bicep-hammer-curl-1.png", 4, "Curl martillo" },
                    { 16, "https://wger.de/media/exercise-images/1649/441cc0e5-eca2-4828-8b0a-a0e554abb2ff.jpg", 4, "Curl concentrado" },
                    { 17, "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png", 5, "Fondos en paralelas" },
                    { 18, "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg", 5, "Extensión con polea" },
                    { 19, "https://wger.de/media/exercise-images/84/Lying-close-grip-triceps-press-to-chin-1.png", 5, "Press francés" },
                    { 20, "https://placehold.co/400x300/EEE/31343C.png?font=montserrat&text=Patada%20triceps", 5, "Patada de tríceps" },
                    { 21, "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg", 6, "Sentadilla" },
                    { 22, "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp", 6, "Prensa" },
                    { 23, "https://wger.de/media/exercise-images/851/4d621b17-f6cb-4107-97c0-9f44e9a2dbc6.webp", 6, "Extensión de cuádriceps" },
                    { 24, "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png", 6, "Curl femoral" },
                    { 25, "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png", 6, "Zancadas" },
                    { 26, "https://wger.de/media/exercise-images/91/Crunches-1.png", 7, "Crunch" },
                    { 27, "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png", 7, "Plancha" },
                    { 28, "https://wger.de/media/exercise-images/125/Leg-raises-2.png", 7, "Elevación de piernas" },
                    { 29, "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png", 7, "Russian twist" },
                    { 30, "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg", 8, "Hip thrust" },
                    { 31, "https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp", 8, "Peso muerto rumano" },
                    { 32, "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg", 8, "Patada de glúteo" },
                    { 33, "https://placehold.co/400x300/EEE/31343C.png?font=montserrat&text=Puente%20gluteos", 8, "Puente de glúteos" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DayExercises_Day_ExerciseId",
                table: "DayExercises",
                columns: new[] { "Day", "ExerciseId" });

            migrationBuilder.CreateIndex(
                name: "IX_DayExercises_ExerciseId",
                table: "DayExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_DayMuscleGroups_Day_MuscleGroupId",
                table: "DayMuscleGroups",
                columns: new[] { "Day", "MuscleGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DayMuscleGroups_MuscleGroupId",
                table: "DayMuscleGroups",
                column: "MuscleGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_MuscleGroupId",
                table: "Exercises",
                column: "MuscleGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExerciseLogs_WorkoutSessionId",
                table: "WorkoutExerciseLogs",
                column: "WorkoutSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DayExercises");

            migrationBuilder.DropTable(
                name: "DayMuscleGroups");

            migrationBuilder.DropTable(
                name: "WorkoutExerciseLogs");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "WorkoutSessions");

            migrationBuilder.DropTable(
                name: "MuscleGroups");
        }
    }
}
