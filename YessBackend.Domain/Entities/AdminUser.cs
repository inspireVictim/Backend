using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YessBackend.Domain.Entities;

/// <summary>
/// Модель администратора системы
/// Соответствует таблице "AdminUsers" в PostgreSQL с UUID и enum admin_role
/// </summary>
public class AdminUser
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Основная информация
    [Required]
    [MaxLength(100)] // VARCHAR(100) в таблице
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    // Роль админа: 'superadmin', 'admin', 'moderator' (enum admin_role в БД)
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "admin";
    
    // Статус
    public bool IsActive { get; set; } = true;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Опциональные поля (можно добавить в таблицу позже, если нужно):
    // public string? Phone { get; set; }
    // public string? FirstName { get; set; }
    // public string? LastName { get; set; }
    // public string? AvatarUrl { get; set; }
    // public bool IsBlocked { get; set; } = false;
    // public DateTime? LastLoginAt { get; set; }
}

