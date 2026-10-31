namespace FitCycle.Core.Models;

public class RoutineExercise
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal Weight { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string MuscleGroupName { get; set; } = string.Empty;
    /// <summary>
    /// JSON array of per-set details: [{"reps":12,"weight":10},{"reps":10,"weight":12.5}]
    /// When empty, all sets use the same Reps/Weight values.
    /// </summary>
    public string SetDetails { get; set; } = string.Empty;
    /// <summary>
    /// Superset group number. Exercises with the same non-zero SupersetGroup are done back-to-back.
    /// </summary>
    public int SupersetGroup { get; set; }
    /// <summary>
    /// Free-text notes/instructions for this exercise (e.g. trainer cues).
    /// </summary>
    public string Notes { get; set; } = string.Empty;
    /// <summary>
    /// Biweekly variant support. ExerciseId/Name/Image describe the exercise to train
    /// THIS week (the repository swaps in the variant on "B" weeks). BaseExerciseId and
    /// AlternateExerciseId are the STORED pair, unaffected by the weekly swap, so the
    /// editor can round-trip them without inverting the cycle.
    /// </summary>
    public int BaseExerciseId { get; set; }
    public int? AlternateExerciseId { get; set; }
    /// <summary>Name of the variant NOT served this week (for the "alterna con: X" badge).</summary>
    public string AlternateExerciseName { get; set; } = string.Empty;
    /// <summary>True when this week serves the alternate variant (week "B").</summary>
    public bool IsAlternateWeek { get; set; }
}
