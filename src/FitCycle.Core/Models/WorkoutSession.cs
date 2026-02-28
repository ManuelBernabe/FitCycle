namespace FitCycle.Core.Models;

public class WorkoutSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DayOfWeek Day { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<WorkoutExerciseLog> ExerciseLogs { get; set; } = [];
}
