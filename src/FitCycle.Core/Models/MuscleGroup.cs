using System.Text.Json.Serialization;

namespace FitCycle.Core.Models;

public class MuscleGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [JsonIgnore]
    public List<Exercise> Exercises { get; set; } = [];
}
