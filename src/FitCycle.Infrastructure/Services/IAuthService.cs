using FitCycle.Core.Models;

namespace FitCycle.Infrastructure.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshRequest request);
    Task<UserInfo?> GetUserInfoAsync(int userId);
    Task<List<UserInfo>> GetAllUsersAsync();
    Task<UserInfo> CreateUserAsync(CreateUserRequest request);
    Task<UserInfo> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(int id, int currentUserId);
    Task ResetPasswordAsync(int id, string newPassword);
}
