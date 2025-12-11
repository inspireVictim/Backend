using Microsoft.AspNetCore.Mvc;
using YessBackend.Application.DTOs.AdminAuth;
using YessBackend.Application.Services;
using YessBackend.Application.DTOs.Auth;

namespace YessBackend.Api.Controllers.v1;

/// <summary>
/// Контроллер аутентификации администратора
/// Использует отдельную таблицу AdminUsers
/// </summary>
[ApiController]
[Route("api/v1/admin/auth")]
[Tags("Admin Authentication")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthService _adminAuthService;
    private readonly ILogger<AdminAuthController> _logger;

    public AdminAuthController(
        IAdminAuthService adminAuthService,
        ILogger<AdminAuthController> logger)
    {
        _adminAuthService = adminAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Аутентификация администратора
    /// POST /api/v1/admin/auth/login
    /// Поддерживает вход по username или email
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] AdminLoginDto loginDto)
    {
        try
        {
            var tokenResponse = await _adminAuthService.LoginAsync(loginDto);
            
            // Получаем информацию об администраторе для ответа
            Domain.Entities.AdminUser? adminUser = null;
            if (loginDto.Username.Contains("@"))
            {
                adminUser = await _adminAuthService.GetAdminByEmailAsync(loginDto.Username);
            }
            else
            {
                adminUser = await _adminAuthService.GetAdminByUsernameAsync(loginDto.Username);
            }

            if (adminUser == null)
            {
                return Unauthorized(new { error = "Администратор не найден" });
            }

            return Ok(new
            {
                access_token = tokenResponse.AccessToken,
                refresh_token = tokenResponse.RefreshToken,
                token_type = tokenResponse.TokenType,
                expires_in = tokenResponse.ExpiresIn,
                admin = new
                {
                    id = adminUser.Id.ToString(),
                    username = adminUser.Username,
                    email = adminUser.Email,
                    role = adminUser.Role,
                    is_active = adminUser.IsActive,
                    name = adminUser.Username
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Ошибка входа администратора: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка входа администратора");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Регистрация нового администратора
    /// POST /api/v1/admin/auth/register
    /// Требует прав супер-администратора (можно добавить авторизацию позже)
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AdminResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminResponseDto>> Register([FromBody] AdminRegisterDto registerDto)
    {
        try
        {
            var adminUser = await _adminAuthService.RegisterAdminAsync(registerDto);

            var responseDto = new AdminResponseDto
            {
                Id = adminUser.Id,
                Username = adminUser.Username,
                Email = adminUser.Email,
                Role = adminUser.Role,
                IsActive = adminUser.IsActive,
                CreatedAt = adminUser.CreatedAt,
                UpdatedAt = adminUser.UpdatedAt
            };

            return CreatedAtAction(nameof(Register), responseDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Ошибка регистрации администратора: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка регистрации администратора");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }
}

