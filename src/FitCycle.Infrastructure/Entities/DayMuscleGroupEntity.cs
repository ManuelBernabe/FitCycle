using FitCycle.Core.Models;

namespace FitCycle.Infrastructure.Entities;

public class DayMuscleGroupEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DayOfWeek Day { get; set; }
    public int MuscleGroupId { get; set; }
    public MuscleGroup? MuscleGroup { get; set; }
}
