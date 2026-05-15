using System.Text.Json.Serialization;

namespace FitCycle.Core.Models;

public class Exercise
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MuscleGroupId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    /// <summary>Optional URL to a YouTube demo video for the movement.</summary>
    public string? VideoUrl { get; set; }
    /// <summary>Cached AI-generated form notes (bullet list). Populated on first request.</summary>
    public string? FormNotes { get; set; }
    [JsonIgnore]
    public MuscleGroup? MuscleGroup { get; set; }
}
