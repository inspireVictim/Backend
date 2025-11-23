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

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<string> SendVerificationCodeAsync(string phoneNumber)
    {
        // Генерируем 6-значный код
        var random = new Random();
        var code = random.Next(100000, 999999).ToString("D6");

        // Проверяем, существует ли пользователь
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == phoneNumber);

        if (user != null)
        {
            // Если пользователь существует, сохраняем код
            // Но только если он еще не зарегистрирован (нет пароля)
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new InvalidOperationException("Пользователь с таким номером телефона уже зарегистрирован");
            }

            user.VerificationCode = code;
            user.VerificationExpiresAt = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Если пользователя нет, создаем временную запись пользователя для сохранения кода
            // Это нужно для проверки кода при регистрации
            var tempUser = new User
            {
                Phone = phoneNumber,
                Email = $"{phoneNumber}@temp.local",
                VerificationCode = code,
                VerificationExpiresAt = DateTime.UtcNow.AddMinutes(10),
                PhoneVerified = false,
                IsActive = false, // Временно неактивен до завершения регистрации
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();
        }

        // TODO: Здесь должна быть отправка SMS через внешний сервис (Twilio и т.д.)
        // Пока возвращаем код для тестирования
        
        return code;
    }

    public async Task<User> VerifyCodeAndRegisterAsync(VerifyCodeAndRegisterRequestDto requestDto)
    {
        // Проверяем код верификации
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == requestDto.PhoneNumber);

        if (user == null)
        {
            throw new InvalidOperationException("Код верификации не найден. Сначала отправьте код верификации.");
        }

        // Проверяем, что код был отправлен (пользователь существует)
        if (string.IsNullOrEmpty(user.VerificationCode))
        {
            throw new InvalidOperationException("Код верификации не найден. Сначала отправьте код верификации.");
        }

        // Проверяем срок действия кода
        if (user.VerificationExpiresAt.HasValue && user.VerificationExpiresAt.Value < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Срок действия кода верификации истек. Отправьте новый код.");
        }

        // Проверяем код
        if (user.VerificationCode != requestDto.Code)
        {
            throw new InvalidOperationException("Неверный код верификации");
        }

        // Проверяем, не зарегистрирован ли уже пользователь (есть ли пароль)
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new InvalidOperationException("Пользователь с таким номером телефона уже зарегистрирован");
        }

        // Обновляем данные пользователя (вместо создания нового)
        user.PasswordHash = HashPassword(requestDto.Password);
        user.FirstName = requestDto.FirstName;
        user.LastName = requestDto.LastName;
        user.Name = $"{requestDto.FirstName} {requestDto.LastName}";
        
        if (requestDto.CityId.HasValue && requestDto.CityId.Value > 0)
        {
            user.CityId = requestDto.CityId.Value;
        }

        // Обрабатываем реферальную систему
        if (!string.IsNullOrEmpty(requestDto.ReferralCode))
        {
            var referredByUser = await _context.Users
                .FirstOrDefaultAsync(u => u.ReferralCode == requestDto.ReferralCode);
            if (referredByUser != null)
            {
                user.ReferredBy = referredByUser.Id;
            }
        }

        // Генерируем уникальный реферальный код для нового пользователя, если его еще нет
        if (string.IsNullOrEmpty(user.ReferralCode))
        {
            user.ReferralCode = GenerateUniqueReferralCode();
        }

        user.VerificationCode = null;
        user.PhoneVerified = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Создаем кошелек для нового пользователя, если его еще нет
        var existingWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == user.Id);

        if (existingWallet == null)
        {
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
        }

        return user;
    }

    public async Task<ReferralStatsResponseDto> GetReferralStatsAsync(int userId)
    {
        // Подсчитываем всех приглашенных пользователей
        var totalReferred = await _context.Users
            .CountAsync(u => u.ReferredBy == userId);

        // Подсчитываем активных приглашенных пользователей (которые заходили за последние 30 дней)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var activeReferred = await _context.Users
            .CountAsync(u => u.ReferredBy == userId && 
                           u.LastLoginAt.HasValue && 
                           u.LastLoginAt.Value >= thirtyDaysAgo);

        // Получаем реферальный код текущего пользователя
        var user = await GetUserByIdAsync(userId);
        var referralCode = user?.ReferralCode;

        return new ReferralStatsResponseDto
        {
            TotalReferred = totalReferred,
            ActiveReferred = activeReferred,
            ReferralCode = referralCode
        };
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
