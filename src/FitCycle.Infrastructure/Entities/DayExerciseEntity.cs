using FitCycle.Core.Models;

namespace FitCycle.Infrastructure.Entities;

public class DayExerciseEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DayOfWeek Day { get; set; }
    public int ExerciseId { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal Weight { get; set; }
    public string SetDetails { get; set; } = string.Empty;
    public int SupersetGroup { get; set; }
    public string Notes { get; set; } = string.Empty;
    /// <summary>0-based position within the day. Persists the user-defined exercise order across the whole day.</summary>
    public int Position { get; set; }
    public Exercise? Exercise { get; set; }
}
