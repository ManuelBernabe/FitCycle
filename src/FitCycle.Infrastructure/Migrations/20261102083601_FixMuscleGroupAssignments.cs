using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCycle.Infrastructure.Migrations
{
    /// <summary>
    /// Data fix: PDF imports trusted the AI's per-exercise muscle label, which tagged
    /// "Femoral ..." as Espalda and "Elevación lateral ..." as Pecho. Reclassifies the
    /// exercise catalog AND the denormalized MuscleGroupName stored on past workout
    /// logs using the same deterministic name keywords the importer now applies.
    /// SQLite lower() only folds ASCII, so each accented keyword is matched in both
    /// its accented and plain spellings.
    /// </summary>
    public partial class FixMuscleGroupAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Glúteos (checked FIRST: "Hiperextensiones Enfoque Glúteo" is glutes, not lumbar)
            migrationBuilder.Sql(@"
UPDATE Exercises SET MuscleGroupId = (SELECT Id FROM MuscleGroups WHERE Name = 'Glúteos')
WHERE EXISTS (SELECT 1 FROM MuscleGroups WHERE Name = 'Glúteos')
  AND (lower(Name) LIKE '%gluteo%' OR lower(Name) LIKE '%glúteo%' OR lower(Name) LIKE '%hip thrust%');
");
            migrationBuilder.Sql(@"
UPDATE WorkoutExerciseLogs SET MuscleGroupName = 'Glúteos'
WHERE (lower(ExerciseName) LIKE '%gluteo%' OR lower(ExerciseName) LIKE '%glúteo%' OR lower(ExerciseName) LIKE '%hip thrust%');
");

            // ── Piernas (femoral/gemelo/abductor/aductor/cuádriceps/...; never overrides a glúteo name)
            migrationBuilder.Sql(@"
UPDATE Exercises SET MuscleGroupId = (SELECT Id FROM MuscleGroups WHERE Name = 'Piernas')
WHERE EXISTS (SELECT 1 FROM MuscleGroups WHERE Name = 'Piernas')
  AND (lower(Name) LIKE '%femoral%' OR lower(Name) LIKE '%isquio%' OR lower(Name) LIKE '%gemelo%'
    OR lower(Name) LIKE '%cuadriceps%' OR lower(Name) LIKE '%cuádriceps%'
    OR lower(Name) LIKE '%abductor%' OR lower(Name) LIKE '%aductor%'
    OR lower(Name) LIKE '%sentadilla%' OR lower(Name) LIKE '%zancada%'
    OR lower(Name) LIKE '%pantorrilla%' OR lower(Name) LIKE '%prensa%')
  AND lower(Name) NOT LIKE '%gluteo%' AND lower(Name) NOT LIKE '%glúteo%';
");
            migrationBuilder.Sql(@"
UPDATE WorkoutExerciseLogs SET MuscleGroupName = 'Piernas'
WHERE (lower(ExerciseName) LIKE '%femoral%' OR lower(ExerciseName) LIKE '%isquio%' OR lower(ExerciseName) LIKE '%gemelo%'
    OR lower(ExerciseName) LIKE '%cuadriceps%' OR lower(ExerciseName) LIKE '%cuádriceps%'
    OR lower(ExerciseName) LIKE '%abductor%' OR lower(ExerciseName) LIKE '%aductor%'
    OR lower(ExerciseName) LIKE '%sentadilla%' OR lower(ExerciseName) LIKE '%zancada%'
    OR lower(ExerciseName) LIKE '%pantorrilla%' OR lower(ExerciseName) LIKE '%prensa%')
  AND lower(ExerciseName) NOT LIKE '%gluteo%' AND lower(ExerciseName) NOT LIKE '%glúteo%';
");

            // ── Hombros (elevaciones laterales/frontales, press militar, pájaros)
            migrationBuilder.Sql(@"
UPDATE Exercises SET MuscleGroupId = (SELECT Id FROM MuscleGroups WHERE Name = 'Hombros')
WHERE EXISTS (SELECT 1 FROM MuscleGroups WHERE Name = 'Hombros')
  AND (lower(Name) LIKE '%elevacion lateral%' OR lower(Name) LIKE '%elevación lateral%'
    OR lower(Name) LIKE '%elevaciones laterales%'
    OR lower(Name) LIKE '%elevacion frontal%' OR lower(Name) LIKE '%elevación frontal%'
    OR lower(Name) LIKE '%press militar%' OR lower(Name) LIKE '%pajaro%' OR lower(Name) LIKE '%pájaro%');
");
            migrationBuilder.Sql(@"
UPDATE WorkoutExerciseLogs SET MuscleGroupName = 'Hombros'
WHERE (lower(ExerciseName) LIKE '%elevacion lateral%' OR lower(ExerciseName) LIKE '%elevación lateral%'
    OR lower(ExerciseName) LIKE '%elevaciones laterales%'
    OR lower(ExerciseName) LIKE '%elevacion frontal%' OR lower(ExerciseName) LIKE '%elevación frontal%'
    OR lower(ExerciseName) LIKE '%press militar%' OR lower(ExerciseName) LIKE '%pajaro%' OR lower(ExerciseName) LIKE '%pájaro%');
");

            // ── Espalda: hiperextensiones sin enfoque glúteo = lumbar
            migrationBuilder.Sql(@"
UPDATE Exercises SET MuscleGroupId = (SELECT Id FROM MuscleGroups WHERE Name = 'Espalda')
WHERE EXISTS (SELECT 1 FROM MuscleGroups WHERE Name = 'Espalda')
  AND (lower(Name) LIKE '%hiperextension%' OR lower(Name) LIKE '%hiperextensión%')
  AND lower(Name) NOT LIKE '%gluteo%' AND lower(Name) NOT LIKE '%glúteo%';
");
            migrationBuilder.Sql(@"
UPDATE WorkoutExerciseLogs SET MuscleGroupName = 'Espalda'
WHERE (lower(ExerciseName) LIKE '%hiperextension%' OR lower(ExerciseName) LIKE '%hiperextensión%')
  AND lower(ExerciseName) NOT LIKE '%gluteo%' AND lower(ExerciseName) NOT LIKE '%glúteo%';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data fix — the previous (incorrect) assignments are not recoverable.
        }
    }
}
