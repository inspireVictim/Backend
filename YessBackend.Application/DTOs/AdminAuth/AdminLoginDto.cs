using System.ComponentModel.DataAnnotations;

namespace YessBackend.Application.DTOs.AdminAuth;

/// <summary>
/// DTO для входа администратора
/// Поддерживает вход по username (email/phone) или email
/// </summary>
public class AdminLoginDto
{
    [Required(ErrorMessage = "Username обязателен")]
    public string Username { get; set; } = string.Empty; // Email или username
    
    [Required(ErrorMessage = "Пароль обязателен")]
    public string Password { get; set; } = string.Empty;
}

