using System.ComponentModel.DataAnnotations;

namespace YessBackend.Application.DTOs.AdminAuth;

/// <summary>
/// DTO для регистрации нового администратора
/// Соответствует структуре таблицы AdminUsers
/// </summary>
public class AdminRegisterDto
{
    [Required(ErrorMessage = "Username обязателен")]
    [MinLength(3, ErrorMessage = "Username должен быть не менее 3 символов")]
    [MaxLength(100, ErrorMessage = "Username не должен превышать 100 символов")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    [MaxLength(255, ErrorMessage = "Email не должен превышать 255 символов")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(8, ErrorMessage = "Пароль должен быть не менее 8 символов")]
    public string Password { get; set; } = string.Empty;
    
    [MaxLength(50)]
    [RegularExpression("^(superadmin|admin|moderator)$", ErrorMessage = "Роль должна быть: superadmin, admin или moderator")]
    public string Role { get; set; } = "admin"; // superadmin, admin, moderator (enum admin_role в БД)
}

