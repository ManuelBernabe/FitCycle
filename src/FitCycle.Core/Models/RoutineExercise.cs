namespace FitCycle.Core.Models;

public class RoutineExercise
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string MuscleGroupName { get; set; } = string.Empty;
}
