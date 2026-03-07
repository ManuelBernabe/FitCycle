namespace FitCycle.Infrastructure.Services;

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, UserInfo User);
public record UserInfo(int Id, string Username, string Email, string Role, bool IsActive);
public record CreateUserRequest(string Username, string Email, string Password, string Role);
public record UpdateUserRequest(string? Username, string? Email, string? Role, bool? IsActive);
public record ResetPasswordRequest(string NewPassword);
public record RegisterResponse(string Message, string ActivationToken);
public record ResendActivationRequest(string Email);

// 2FA DTOs
public record TwoFactorLoginResponse(bool Requires2FA, string TempToken);
public record Verify2FARequest(string TempToken, string Code);
public record TwoFactorSetupResponse(string Secret, string OtpAuthUri);
public record Confirm2FARequest(string Code);
public record Disable2FARequest(string Password);
public record TwoFactorStatusResponse(bool Enabled);
public record TwoFactorConfirmResponse(string[] RecoveryCodes);
