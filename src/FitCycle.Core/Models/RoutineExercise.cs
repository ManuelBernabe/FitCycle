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
}
