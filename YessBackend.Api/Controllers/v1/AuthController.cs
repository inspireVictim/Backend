using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YessBackend.Application.DTOs.Auth;
using YessBackend.Application.Services;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;

namespace YessBackend.Api.Controllers.v1;

/// <summary>
/// Контроллер аутентификации
/// Соответствует /api/v1/auth из Python API
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        IMapper mapper,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// POST /api/v1/auth/register
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponseDto>> Register([FromBody] UserRegisterDto registerDto)
    {
        try
        {
            var user = await _authService.RegisterUserAsync(registerDto);
            var response = _mapper.Map<UserResponseDto>(user);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ошибка регистрации пользователя");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Вход пользователя (JSON)
    /// POST /api/v1/auth/login
    /// Поддерживает JSON формат
    /// </summary>
    [HttpPost("login")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] UserLoginDto loginDto)
    {
        try
        {
            var tokenResponse = await _authService.LoginAsync(loginDto);
            return Ok(tokenResponse);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ошибка входа пользователя");
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Вход пользователя (JSON)
    /// POST /api/v1/auth/login/json
    /// Поддерживает JSON формат
    /// </summary>
    [HttpPost("login/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> LoginJson([FromBody] UserLoginDto loginDto)
    {
        try
        {
            var tokenResponse = await _authService.LoginAsync(loginDto);
            return Ok(tokenResponse);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ошибка входа пользователя");
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Обновление access/refresh токенов по refresh токену
    /// POST /api/v1/auth/refresh
    /// </summary>
    [HttpPost("refresh")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error = "refresh_token is required" });
        }

        try
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var secretKey = jwtSection["SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey не настроен");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(request.RefreshToken, validationParameters, out var _);

            var typeClaim = principal.FindFirst("type")?.Value;
            if (!string.Equals(typeClaim, "refresh", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { error = "Invalid token type" });
            }

            var phone = principal.FindFirst("phone")?.Value ?? principal.Identity?.Name;
            if (string.IsNullOrEmpty(phone))
            {
                return Unauthorized(new { error = "Invalid refresh token" });
            }

            var user = await _authService.GetUserByPhoneAsync(phone);
            if (user == null || !user.IsActive || user.IsBlocked)
            {
                return Unauthorized(new { error = "Пользователь не найден или заблокирован" });
            }

            var accessToken = _authService.CreateAccessToken(user);
            var refreshToken = _authService.CreateRefreshToken(user);
            var expiresMinutes = jwtSection.GetValue<int>("AccessTokenExpireMinutes", 60);

            var response = new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "bearer",
                ExpiresIn = expiresMinutes * 60
            };

            return Ok(response);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Неверный refresh токен");
            return Unauthorized(new { error = "Неверный refresh токен" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении токена");
            return StatusCode(500, new { error = "Ошибка сервера при обновлении токена" });
        }
    }

    /// <summary>
    /// Получить информацию о текущем пользователе
    /// GET /api/v1/auth/me
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<UserResponseDto>> GetMe()
    {
        try
        {
            var userId = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Task.FromResult<ActionResult<UserResponseDto>>(Unauthorized(new { error = "Неверный токен" }));
            }

            // TODO: Реализовать получение пользователя по ID
            // var user = await _authService.GetUserByIdAsync(int.Parse(userId));
            // var response = _mapper.Map<UserResponseDto>(user);
            // return Ok(response);
            
            return Task.FromResult<ActionResult<UserResponseDto>>(Ok(new { message = "Endpoint в разработке" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения информации о пользователе");
            return Task.FromResult<ActionResult<UserResponseDto>>(Unauthorized(new { error = "Ошибка аутентификации" }));
        }
    }

    /// <summary>
    /// Отправка SMS кода (заглушка)
    /// POST /api/v1/auth/send-verification-code
    /// </summary>
    [HttpPost("send-verification-code")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult SendVerificationCode([FromBody] SendVerificationCodeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new { error = "phone_number is required" });
        }

        const string code = "123456";
        _logger.LogInformation("Отправка SMS кода (заглушка) на номер {Phone}. Код: {Code}", request.PhoneNumber, code);

        return Ok(new
        {
            phone_number = request.PhoneNumber,
            code,
            message = "Код отправлен (заглушка)",
            success = true
        });
    }

    /// <summary>
    /// DTO для запроса refresh токена
    /// </summary>
    public class RefreshTokenRequestDto
    {
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO для запроса отправки SMS кода
    /// </summary>
    public class SendVerificationCodeRequestDto
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
