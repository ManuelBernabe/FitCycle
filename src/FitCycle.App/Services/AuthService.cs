using System.Net.Http.Json;
using System.Text.Json;

namespace FitCycle.App.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task<AuthResult> RegisterAsync(string username, string email, string password);
    Task<bool> RefreshTokenAsync();
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetAccessTokenAsync();
    Task<UserInfoResult?> GetCurrentUserInfoAsync();
    Task<List<UserInfoResult>> GetAllUsersAsync();
    Task<UserInfoResult?> CreateUserAsync(string username, string email, string password, string role);
    Task<UserInfoResult?> UpdateUserAsync(int id, string? username, string? email, string? role);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> ResetPasswordAsync(int id, string newPassword);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
}

public class UserInfoResult
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string AccessTokenKey = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";
    private const string UsernameKey = "auth_username";
    private const string RoleKey = "auth_role";

    public AuthService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            var body = new { Username = username, Password = password };
            var resp = await _http.PostAsJsonAsync("/auth/login", body);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
                return new AuthResult { Success = false, Error = err?.Error ?? "Error al iniciar sesión" };
            }

            var result = await resp.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
            if (result is null)
                return new AuthResult { Success = false, Error = "Respuesta inválida del servidor" };

            await StoreTokensAsync(result);
            return new AuthResult
            {
                Success = true,
                Username = result.User?.Username,
                Role = result.User?.Role
            };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, Error = $"Error de conexión: {ex.Message}" };
        }
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string password)
    {
        try
        {
            var body = new { Username = username, Email = email, Password = password };
            var resp = await _http.PostAsJsonAsync("/auth/register", body);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
                return new AuthResult { Success = false, Error = err?.Error ?? "Error al registrarse" };
            }

            var result = await resp.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
            if (result is null)
                return new AuthResult { Success = false, Error = "Respuesta inválida del servidor" };

            await StoreTokensAsync(result);
            return new AuthResult
            {
                Success = true,
                Username = result.User?.Username,
                Role = result.User?.Role
            };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, Error = $"Error de conexión: {ex.Message}" };
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await SecureStorage.GetAsync(RefreshTokenKey);
            if (string.IsNullOrEmpty(refreshToken)) return false;

            var body = new { RefreshToken = refreshToken };
            var resp = await _http.PostAsJsonAsync("/auth/refresh", body);
            if (!resp.IsSuccessStatusCode) return false;

            var result = await resp.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
            if (result is null) return false;

            await StoreTokensAsync(result);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task LogoutAsync()
    {
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
        SecureStorage.Remove(UsernameKey);
        SecureStorage.Remove(RoleKey);
        return Task.CompletedTask;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await SecureStorage.GetAsync(AccessTokenKey);
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await SecureStorage.GetAsync(AccessTokenKey);
    }

    public async Task<UserInfoResult?> GetCurrentUserInfoAsync()
    {
        try
        {
            var resp = await _http.GetAsync("/auth/me");
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<UserInfoResult>(JsonOptions);
        }
        catch { return null; }
    }

    public async Task<List<UserInfoResult>> GetAllUsersAsync()
    {
        try
        {
            var resp = await _http.GetAsync("/users");
            if (!resp.IsSuccessStatusCode) return [];
            return await resp.Content.ReadFromJsonAsync<List<UserInfoResult>>(JsonOptions) ?? [];
        }
        catch { return []; }
    }

    public async Task<UserInfoResult?> CreateUserAsync(string username, string email, string password, string role)
    {
        try
        {
            var body = new { Username = username, Email = email, Password = password, Role = role };
            var resp = await _http.PostAsJsonAsync("/users", body);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<UserInfoResult>(JsonOptions);
        }
        catch { return null; }
    }

    public async Task<UserInfoResult?> UpdateUserAsync(int id, string? username, string? email, string? role)
    {
        try
        {
            var body = new { Username = username, Email = email, Role = role };
            var resp = await _http.PutAsJsonAsync($"/users/{id}", body);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<UserInfoResult>(JsonOptions);
        }
        catch { return null; }
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        try
        {
            var resp = await _http.DeleteAsync($"/users/{id}");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> ResetPasswordAsync(int id, string newPassword)
    {
        try
        {
            var body = new { NewPassword = newPassword };
            var resp = await _http.PutAsJsonAsync($"/users/{id}/password", body);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private async Task StoreTokensAsync(TokenResponse response)
    {
        await SecureStorage.SetAsync(AccessTokenKey, response.AccessToken);
        await SecureStorage.SetAsync(RefreshTokenKey, response.RefreshToken);
        if (response.User is not null)
        {
            await SecureStorage.SetAsync(UsernameKey, response.User.Username);
            await SecureStorage.SetAsync(RoleKey, response.User.Role);
        }
    }

    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserInfoDto? User { get; set; }
    }

    private class UserInfoDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    private class ErrorResponse
    {
        public string? Error { get; set; }
    }
}
