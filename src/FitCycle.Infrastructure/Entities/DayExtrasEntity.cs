namespace FitCycle.Infrastructure.Entities;

public class DayExtrasEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DayOfWeek Day { get; set; }
    public string CardioType { get; set; } = string.Empty;
    public int CardioMinutes { get; set; }
    public string AbsExercise { get; set; } = string.Empty;
    public int AbsMinutes { get; set; }
}
