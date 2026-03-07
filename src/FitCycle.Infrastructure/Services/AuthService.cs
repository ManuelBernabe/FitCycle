using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FitCycle.Core.Models;
using FitCycle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OtpNet;

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

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            throw new ArgumentException("El nombre de usuario debe tener al menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new ArgumentException("El email no es válido.");
        ValidatePasswordStrength(request.Password);

        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            throw new ArgumentException("El nombre de usuario ya está en uso.");
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new ArgumentException("El email ya está registrado.");

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var activationToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Standard,
            CreatedAt = DateTime.UtcNow,
            IsActive = false,
            ActivationToken = activationToken,
            ActivationTokenExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new RegisterResponse("Registro exitoso. Revisa tu email para activar tu cuenta.", activationToken);
    }

    public async Task<object> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tu cuenta no está activada. Revisa tu email para activarla.");

        if (user.TwoFactorEnabled)
        {
            var tempToken = GenerateTempToken(user);
            return new TwoFactorLoginResponse(true, tempToken);
        }

        return GenerateTokens(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.RefreshToken == request.RefreshToken &&
            u.RefreshTokenExpiresAt > DateTime.UtcNow &&
            u.IsActive);

        if (user is null)
            throw new UnauthorizedAccessException("Refresh token inválido o expirado.");

        return GenerateTokens(user);
    }

    public async Task<UserInfo?> GetUserInfoAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return null;
        return new UserInfo(user.Id, user.Username, user.Email, user.Role.ToString(), user.IsActive);
    }

    public async Task<List<UserInfo>> GetAllUsersAsync()
    {
        return await _db.Users
            .OrderBy(u => u.Id)
            .Select(u => new UserInfo(u.Id, u.Username, u.Email, u.Role.ToString(), u.IsActive))
            .ToListAsync();
    }

    public async Task<UserInfo> CreateUserAsync(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            throw new ArgumentException("El nombre de usuario debe tener al menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new ArgumentException("El email no es válido.");
        ValidatePasswordStrength(request.Password);

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
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new UserInfo(user.Id, user.Username, user.Email, user.Role.ToString(), user.IsActive);
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

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
            if (user.IsActive)
            {
                user.ActivationToken = null;
                user.ActivationTokenExpiresAt = null;
            }
        }

        await _db.SaveChangesAsync();

        return new UserInfo(user.Id, user.Username, user.Email, user.Role.ToString(), user.IsActive);
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
        ValidatePasswordStrength(newPassword);

        var user = await _db.Users.FindAsync(id);
        if (user is null)
            throw new ArgumentException("Usuario no encontrado.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
    }

    public async Task<AuthResponse> ImpersonateAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new ArgumentException("Usuario no encontrado.");

        return GenerateTokens(user);
    }

    public async Task<bool> ActivateAsync(string token)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.ActivationToken == token &&
            u.ActivationTokenExpiresAt > DateTime.UtcNow);

        if (user is null) return false;

        user.IsActive = true;
        user.ActivationToken = null;
        user.ActivationTokenExpiresAt = null;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string?> ResendActivationAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == email && !u.IsActive);

        if (user is null) return null;

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var activationToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        user.ActivationToken = activationToken;
        user.ActivationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        await _db.SaveChangesAsync();

        return activationToken;
    }

    // -- 2FA Methods --

    public async Task<AuthResponse> Verify2FAAsync(Verify2FARequest request)
    {
        // Validate temp token
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));

        ClaimsPrincipal principal;
        try
        {
            principal = handler.ValidateToken(request.TempToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwt.Issuer,
                ValidAudience = _jwt.Audience,
                IssuerSigningKey = key
            }, out _);
        }
        catch
        {
            throw new UnauthorizedAccessException("Token temporal inválido o expirado.");
        }

        var purposeClaim = principal.FindFirst("purpose")?.Value;
        if (purposeClaim != "2fa")
            throw new UnauthorizedAccessException("Token temporal inválido.");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Token temporal inválido.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null || !user.TwoFactorEnabled || user.TwoFactorSecret is null)
            throw new UnauthorizedAccessException("2FA no configurado.");

        // Try TOTP code first
        if (ValidateTotpCode(user.TwoFactorSecret, request.Code))
            return GenerateTokens(user);

        // Try recovery code
        if (await ValidateRecoveryCodeAsync(user, request.Code))
            return GenerateTokens(user);

        throw new UnauthorizedAccessException("Código inválido.");
    }

    public TwoFactorSetupResponse Setup2FA(User user)
    {
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(secretBytes);
        user.TwoFactorSecret = secret;
        _db.SaveChanges();

        var otpAuthUri = $"otpauth://totp/FitCycle:{Uri.EscapeDataString(user.Username)}?secret={secret}&issuer=FitCycle&digits=6&period=30";
        return new TwoFactorSetupResponse(secret, otpAuthUri);
    }

    public TwoFactorConfirmResponse Confirm2FA(User user, string code)
    {
        if (user.TwoFactorSecret is null)
            throw new ArgumentException("Primero debes iniciar la configuración de 2FA.");

        if (!ValidateTotpCode(user.TwoFactorSecret, code))
            throw new ArgumentException("Código inválido. Verifica que tu app authenticator está sincronizada.");

        user.TwoFactorEnabled = true;

        // Generate 8 recovery codes
        var codes = new string[8];
        for (int i = 0; i < 8; i++)
        {
            var bytes = RandomNumberGenerator.GetBytes(4);
            var part1 = Convert.ToHexString(bytes[..2]).ToUpperInvariant();
            var part2 = Convert.ToHexString(bytes[2..]).ToUpperInvariant();
            codes[i] = $"{part1}-{part2}";
        }

        // Store hashed recovery codes
        var hashed = codes.Select(c => BCrypt.Net.BCrypt.HashPassword(c.Replace("-", "").ToUpperInvariant())).ToArray();
        user.RecoveryCodes = JsonSerializer.Serialize(hashed);
        _db.SaveChanges();

        return new TwoFactorConfirmResponse(codes);
    }

    public void Disable2FA(User user, string password)
    {
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new ArgumentException("Contraseña incorrecta.");

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.RecoveryCodes = null;
        _db.SaveChanges();
    }

    private bool ValidateTotpCode(string secret, string code)
    {
        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes, step: 30, totpSize: 6);
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ValidateRecoveryCodeAsync(User user, string code)
    {
        if (user.RecoveryCodes is null) return false;

        var normalizedCode = code.Replace("-", "").ToUpperInvariant();
        var hashed = JsonSerializer.Deserialize<string[]>(user.RecoveryCodes);
        if (hashed is null) return false;

        for (int i = 0; i < hashed.Length; i++)
        {
            if (hashed[i] != "" && BCrypt.Net.BCrypt.Verify(normalizedCode, hashed[i]))
            {
                // Consume the code
                hashed[i] = "";
                user.RecoveryCodes = JsonSerializer.Serialize(hashed);
                await _db.SaveChangesAsync();
                return true;
            }
        }

        return false;
    }

    private string GenerateTempToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("purpose", "2fa")
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        if (!Regex.IsMatch(password, @"[A-Z]"))
            throw new ArgumentException("La contraseña debe contener al menos una letra mayúscula.");
        if (!Regex.IsMatch(password, @"[a-z]"))
            throw new ArgumentException("La contraseña debe contener al menos una letra minúscula.");
        if (!Regex.IsMatch(password, @"\d"))
            throw new ArgumentException("La contraseña debe contener al menos un dígito.");
        if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
            throw new ArgumentException("La contraseña debe contener al menos un carácter especial.");
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

        var userInfo = new UserInfo(user.Id, user.Username, user.Email, user.Role.ToString(), user.IsActive);
        return new AuthResponse(accessToken, refreshToken, userInfo);
    }
}
