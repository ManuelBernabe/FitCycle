using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FitCycle.Core.Models;
using FitCycle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FitCycle.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly FitCycleDbContext _db;
    private readonly JwtSettings _jwt;

    public AuthService(FitCycleDbContext db, JwtSettings jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            throw new ArgumentException("El nombre de usuario debe tener al menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new ArgumentException("El email no es válido.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ArgumentException("La contraseña debe tener al menos 6 caracteres.");

        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            throw new ArgumentException("El nombre de usuario ya está en uso.");
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new ArgumentException("El email ya está registrado.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Standard,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return GenerateTokens(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        return GenerateTokens(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.RefreshToken == request.RefreshToken &&
            u.RefreshTokenExpiresAt > DateTime.UtcNow);

        if (user is null)
            throw new UnauthorizedAccessException("Refresh token inválido o expirado.");

        return GenerateTokens(user);
    }

    public async Task<UserInfo?> GetUserInfoAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return null;
        return new UserInfo(user.Id, user.Username, user.Email, user.Role.ToString());
    }

    public async Task<List<UserInfo>> GetAllUsersAsync()
    {
        return await _db.Users
            .OrderBy(u => u.Id)
            .Select(u => new UserInfo(u.Id, u.Username, u.Email, u.Role.ToString()))
            .ToListAsync();
    }

    public async Task<UserInfo> CreateUserAsync(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            throw new ArgumentException("El nombre de usuario debe tener al menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new ArgumentException("El email no es válido.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ArgumentException("La contraseña debe tener al menos 6 caracteres.");

        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            throw new ArgumentException("El nombre de usuario ya está en uso.");
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new ArgumentException("El email ya está registrado.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new ArgumentException("Rol inválido. Valores válidos: Standard, Admin, Superuser.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new UserInfo(user.Id, user.Username, user.Email, user.Role.ToString());
    }

    public async Task<UserInfo> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
            throw new ArgumentException("Usuario no encontrado.");

        if (request.Username is not null)
        {
            if (request.Username.Length < 3)
                throw new ArgumentException("El nombre de usuario debe tener al menos 3 caracteres.");
            if (await _db.Users.AnyAsync(u => u.Username == request.Username && u.Id != id))
                throw new ArgumentException("El nombre de usuario ya está en uso.");
            user.Username = request.Username;
        }

        if (request.Email is not null)
        {
            if (!request.Email.Contains('@'))
                throw new ArgumentException("El email no es válido.");
            if (await _db.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                throw new ArgumentException("El email ya está registrado.");
            user.Email = request.Email;
        }

        if (request.Role is not null)
        {
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
                throw new ArgumentException("Rol inválido. Valores válidos: Standard, Admin, Superuser.");
            user.Role = role;
        }

        await _db.SaveChangesAsync();

        return new UserInfo(user.Id, user.Username, user.Email, user.Role.ToString());
    }

    public async Task<bool> DeleteUserAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
            throw new ArgumentException("No puedes eliminarte a ti mismo.");

        var user = await _db.Users.FindAsync(id);
        if (user is null)
            return false;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ResetPasswordAsync(int id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new ArgumentException("La contraseña debe tener al menos 6 caracteres.");

        var user = await _db.Users.FindAsync(id);
        if (user is null)
            throw new ArgumentException("Usuario no encontrado.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
    }

    private AuthResponse GenerateTokens(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes),
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays);
        _db.SaveChanges();

        var userInfo = new UserInfo(user.Id, user.Username, user.Email, user.Role.ToString());
        return new AuthResponse(accessToken, refreshToken, userInfo);
    }
}
