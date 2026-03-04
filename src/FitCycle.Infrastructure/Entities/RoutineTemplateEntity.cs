namespace FitCycle.Infrastructure.Entities;

public class RoutineTemplateEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public string RoutineDataJson { get; set; } = string.Empty;
}
