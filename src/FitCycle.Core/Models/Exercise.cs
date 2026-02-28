namespace FitCycle.Core.Models;

public class Exercise
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MuscleGroupId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
