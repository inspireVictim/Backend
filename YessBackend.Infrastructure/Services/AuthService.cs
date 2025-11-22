using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using YessBackend.Application.DTOs.Auth;
using YessBackend.Application.Services;
using YessBackend.Domain.Entities;
using YessBackend.Infrastructure.Data;

namespace YessBackend.Infrastructure.Services;

/// <summary>
/// Сервис аутентификации
/// Реализует логику из Python AuthService
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<User> RegisterUserAsync(UserRegisterDto userDto)
    {
        // Проверка существования пользователя
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == userDto.PhoneNumber);
        
        if (existingUser != null)
        {
            throw new InvalidOperationException("Пользователь с таким номером телефона уже существует");
        }

        // Генерация реферального кода
        var referralCode = GenerateUniqueReferralCode();

        // Нормализуем город: 0 или отрицательное значение считаем отсутствием города
        int? cityId = null;
        if (userDto.CityId.HasValue && userDto.CityId.Value > 0)
        {
            cityId = userDto.CityId.Value;
        }

        // Создание пользователя
        var user = new User
        {
            Email = $"{userDto.PhoneNumber}@example.local",
            Phone = userDto.PhoneNumber,
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            PasswordHash = HashPassword(userDto.Password),
            CityId = cityId,
            ReferralCode = referralCode,
            PhoneVerified = false,
            EmailVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Обработка реферального кода
        if (!string.IsNullOrEmpty(userDto.ReferralCode))
        {
            var referrer = await _context.Users
                .FirstOrDefaultAsync(u => u.ReferralCode == userDto.ReferralCode);
            if (referrer != null)
            {
                user.ReferredBy = referrer.Id;
            }
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Создание кошелька
        var wallet = new Wallet
        {
            UserId = user.Id,
            Balance = 0.0m,
            YescoinBalance = 0.0m,
            TotalEarned = 0.0m,
            TotalSpent = 0.0m,
            LastUpdated = DateTime.UtcNow
        };

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<TokenResponseDto> LoginAsync(UserLoginDto loginDto)
    {
        var user = await GetUserByPhoneAsync(loginDto.Phone);
        
        if (user == null)
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        if (!VerifyPassword(loginDto.Password, user.PasswordHash ?? string.Empty))
        {
            throw new InvalidOperationException("Неверный пароль");
        }

        // Обновляем время последнего входа
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var accessToken = CreateAccessToken(user);
        var refreshToken = CreateRefreshToken(user);
        
        var expiresIn = _configuration.GetValue<int>("Jwt:AccessTokenExpireMinutes", 60);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "bearer",
            ExpiresIn = expiresIn * 60 // в секундах
        };
    }

    public async Task<User?> GetUserByPhoneAsync(string phone)
    {
        // Прямая загрузка пользователя по телефону
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == phone);
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Пароль не может быть пустым", nameof(password));
        }

        // Используем BCrypt как в Python версии
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    public string CreateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey не настроен");
        var issuer = jwtSettings["Issuer"] ?? "yess-loyalty";
        var audience = jwtSettings["Audience"] ?? "yess-loyalty";
        var expiresMinutes = jwtSettings.GetValue<int>("AccessTokenExpireMinutes", 60);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Phone),
            new Claim("phone", user.Phone),
            new Claim("user_id", user.Id.ToString())
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

    public string CreateRefreshToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey не настроен");
        var issuer = jwtSettings["Issuer"] ?? "yess-loyalty";
        var audience = jwtSettings["Audience"] ?? "yess-loyalty";
        var expiresDays = jwtSettings.GetValue<int>("RefreshTokenExpireDays", 7);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Phone),
            new Claim("phone", user.Phone),
            new Claim("user_id", user.Id.ToString()),
            new Claim("type", "refresh")
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

    private string GenerateUniqueReferralCode(int length = 8)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var maxAttempts = 100;

        for (int i = 0; i < maxAttempts; i++)
        {
            var code = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            var exists = _context.Users.Any(u => u.ReferralCode == code);
            if (!exists)
            {
                return code;
            }
        }

        // Если не удалось сгенерировать уникальный код, используем timestamp
        var timestampCode = new string(Enumerable.Repeat(chars, length - 4)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        timestampCode += timestamp.Substring(Math.Max(0, timestamp.Length - 4));
        return timestampCode;
    }
}
