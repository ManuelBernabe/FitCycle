using System.Text.Json.Serialization;

namespace FitCycle.Core.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Standard;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [JsonIgnore]
    public string? RefreshToken { get; set; }
    [JsonIgnore]
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    [JsonIgnore]
    public string? ActivationToken { get; set; }
    [JsonIgnore]
    public DateTime? ActivationTokenExpiresAt { get; set; }
    public bool TwoFactorEnabled { get; set; } = false;
    [JsonIgnore]
    public string? TwoFactorSecret { get; set; }
    [JsonIgnore]
    public string? RecoveryCodes { get; set; }
}
