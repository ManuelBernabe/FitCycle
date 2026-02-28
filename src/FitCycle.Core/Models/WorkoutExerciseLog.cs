using System.Text.Json.Serialization;

namespace FitCycle.Core.Models;

public class WorkoutExerciseLog
{
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal Weight { get; set; }
    public string MuscleGroupName { get; set; } = string.Empty;
    /// <summary>
    /// JSON array of per-set details: [{"reps":12,"weight":10},{"reps":10,"weight":12.5}]
    /// </summary>
    public string SetDetails { get; set; } = string.Empty;
    [JsonIgnore]
    public WorkoutSession? WorkoutSession { get; set; }
}
