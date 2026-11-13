namespace FitCycle.Infrastructure.Entities;

/// <summary>
/// Long-lived personal key that lets an Apple Watch (via an iOS Shortcut) drive the
/// user's live workout. Shortcuts can't refresh JWTs, so the watch authenticates with
/// this opaque token instead. Only the SHA-256 hash is stored; one token per user
/// (regenerating replaces it, which instantly revokes the old shortcut URLs).
/// </summary>
public class WatchTokenEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
