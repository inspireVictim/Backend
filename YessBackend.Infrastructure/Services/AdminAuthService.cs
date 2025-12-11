using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using YessBackend.Application.DTOs.AdminAuth;
using YessBackend.Application.DTOs.Auth;
using YessBackend.Application.Services;
using YessBackend.Domain.Entities;
using YessBackend.Infrastructure.Data;

namespace YessBackend.Infrastructure.Services;

/// <summary>
/// Сервис аутентификации администраторов
/// </summary>
public class AdminAuthService : IAdminAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminAuthService>? _logger;

    public AdminAuthService(
        ApplicationDbContext context, 
        IConfiguration configuration, 
        ILogger<AdminAuthService>? logger = null)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AdminUser> RegisterAdminAsync(AdminRegisterDto registerDto)
    {
        // Проверка существования администратора
        var existingByUsername = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Username == registerDto.Username);
        
        if (existingByUsername != null)
        {
            throw new InvalidOperationException("Администратор с таким username уже существует");
        }

        var existingByEmail = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        
        if (existingByEmail != null)
        {
            throw new InvalidOperationException("Администратор с таким email уже существует");
        }

        // Валидация роли (должна быть одна из: superadmin, admin, moderator)
        var validRoles = new[] { "superadmin", "admin", "moderator" };
        var role = !string.IsNullOrWhiteSpace(registerDto.Role) && validRoles.Contains(registerDto.Role.ToLower())
            ? registerDto.Role.ToLower()
            : "admin";

        // Создание администратора
        var adminUser = new AdminUser
        {
            Id = Guid.NewGuid(), // UUID будет сгенерирован в БД, но можем задать явно
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = HashPassword(registerDto.Password),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AdminUsers.Add(adminUser);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Создан новый администратор: {Username} (ID: {Id})", 
            adminUser.Username, adminUser.Id);

        return adminUser;
    }

    public async Task<TokenResponseDto> LoginAsync(AdminLoginDto loginDto)
    {
        // Ищем администратора по username или email
        AdminUser? adminUser = null;
        
        if (loginDto.Username.Contains("@"))
        {
            // Если содержит @, ищем по email
            adminUser = await GetAdminByEmailAsync(loginDto.Username);
        }
        else
        {
            // Иначе ищем по username
            adminUser = await GetAdminByUsernameAsync(loginDto.Username);
        }

        if (adminUser == null)
        {
            throw new InvalidOperationException("Администратор не найден");
        }

        if (!adminUser.IsActive)
        {
            throw new InvalidOperationException("Учетная запись администратора неактивна");
        }

        if (!VerifyPassword(loginDto.Password, adminUser.PasswordHash))
        {
            throw new InvalidOperationException("Неверный пароль");
        }

        // Обновляем время последнего обновления
        adminUser.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var accessToken = CreateAccessToken(adminUser);
        var refreshToken = CreateRefreshToken(adminUser);
        
        var expiresIn = _configuration.GetValue<int>("Jwt:AccessTokenExpireMinutes", 60);

        _logger?.LogInformation("Успешный вход администратора: {Username} (ID: {Id})", 
            adminUser.Username, adminUser.Id);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "bearer",
            ExpiresIn = expiresIn * 60 // в секундах
        };
    }

    public async Task<AdminUser?> GetAdminByUsernameAsync(string username)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<AdminUser?> GetAdminByEmailAsync(string email)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<AdminUser?> GetAdminByIdAsync(Guid adminId)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == adminId);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public string CreateAccessToken(AdminUser adminUser)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey не настроен");
        var issuer = jwtSettings["Issuer"] ?? "yess-loyalty";
        var audience = jwtSettings["Audience"] ?? "yess-loyalty";
        var expiresMinutes = jwtSettings.GetValue<int>("AccessTokenExpireMinutes", 60);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
            new Claim(ClaimTypes.Name, adminUser.Username),
            new Claim(ClaimTypes.Email, adminUser.Email),
            new Claim("admin_id", adminUser.Id.ToString()),
            new Claim("username", adminUser.Username),
            new Claim("email", adminUser.Email),
            new Claim("role", adminUser.Role),
            new Claim("type", "admin") // Тип токена для различения админов от обычных пользователей
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken(AdminUser adminUser)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey не настроен");
        var issuer = jwtSettings["Issuer"] ?? "yess-loyalty";
        var audience = jwtSettings["Audience"] ?? "yess-loyalty";

        // Поддержка как дней, так и минут (для обратной совместимости с Docker)
        var expiresMinutes = jwtSettings.GetValue<int>("RefreshTokenExpireMinutes", -1);
        double expiresDays;

        if (expiresMinutes > 0)
        {
            expiresDays = expiresMinutes / (24.0 * 60.0);
            _logger?.LogDebug("Using RefreshTokenExpireMinutes: {Minutes} minutes = {Days} days", expiresMinutes, expiresDays);
        }
        else
        {
            expiresDays = jwtSettings.GetValue<int>("RefreshTokenExpireDays", 7);
            _logger?.LogDebug("Using RefreshTokenExpireDays: {Days} days", expiresDays);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
            new Claim(ClaimTypes.Name, adminUser.Username),
            new Claim("admin_id", adminUser.Id.ToString()),
            new Claim("username", adminUser.Username),
            new Claim("type", "refresh_admin") // Тип токена для refresh токенов админов
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiresDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

