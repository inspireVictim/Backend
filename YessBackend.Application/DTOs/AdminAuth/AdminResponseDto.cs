namespace YessBackend.Application.DTOs.AdminAuth;

/// <summary>
/// DTO для ответа с информацией об администраторе
/// Соответствует структуре таблицы AdminUsers
/// </summary>
public class AdminResponseDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "admin";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

