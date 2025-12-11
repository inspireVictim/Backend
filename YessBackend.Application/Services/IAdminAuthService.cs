using YessBackend.Application.DTOs.AdminAuth;
using YessBackend.Application.DTOs.Auth;
using YessBackend.Domain.Entities;

namespace YessBackend.Application.Services;

/// <summary>
/// Интерфейс сервиса аутентификации администраторов
/// </summary>
public interface IAdminAuthService
{
    Task<AdminUser> RegisterAdminAsync(AdminRegisterDto registerDto);
    Task<TokenResponseDto> LoginAsync(AdminLoginDto loginDto);
    Task<AdminUser?> GetAdminByUsernameAsync(string username);
    Task<AdminUser?> GetAdminByEmailAsync(string email);
    Task<AdminUser?> GetAdminByIdAsync(Guid adminId);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string CreateAccessToken(AdminUser adminUser);
    string CreateRefreshToken(AdminUser adminUser);
}

