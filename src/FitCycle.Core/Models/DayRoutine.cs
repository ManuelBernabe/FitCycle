namespace FitCycle.Core.Models;

public class DayRoutine
{
    public DayOfWeek Day { get; set; }
    public List<MuscleGroup> MuscleGroups { get; set; } = [];
    public List<RoutineExercise> Exercises { get; set; } = [];
    public string CardioType { get; set; } = string.Empty;
    public int CardioMinutes { get; set; }
    public string AbsExercise { get; set; } = string.Empty;
    public int AbsSets { get; set; }
    public int AbsReps { get; set; }
}
