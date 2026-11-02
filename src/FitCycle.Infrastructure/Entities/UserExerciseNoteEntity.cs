namespace FitCycle.Infrastructure.Entities;

/// <summary>
/// Personal note a user keeps on an exercise ("seat at position 4", "shoulder felt
/// off last time"). Keyed by exercise NAME, not id: PDF re-imports create new
/// Exercise rows, and the note must survive them the same way weight history does.
/// </summary>
public class UserExerciseNoteEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
