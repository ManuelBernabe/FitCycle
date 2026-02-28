namespace FitCycle.Core.Models;

public class DayRoutine
{
    public DayOfWeek Day { get; set; }
    public List<MuscleGroup> MuscleGroups { get; set; } = [];
    public List<RoutineExercise> Exercises { get; set; } = [];
}
