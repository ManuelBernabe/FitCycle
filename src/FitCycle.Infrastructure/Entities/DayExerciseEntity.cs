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
    public Exercise? Exercise { get; set; }
}
