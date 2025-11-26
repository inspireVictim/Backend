using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YessBackend.Domain.Entities;

/// <summary>
/// Реестр платежей для ежедневной сверки с Optima Bank
/// Соответствует требованиям QIWI OSMP v1.4
/// </summary>
public class ReconciliationReport
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Дата реестра (за какой день)
    /// </summary>
    [Required]
    public DateTime ReportDate { get; set; }
    
    /// <summary>
    /// Дата и время генерации реестра
    /// </summary>
    [Required]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Email адрес, на который отправлен реестр
    /// </summary>
    [MaxLength(255)]
    public string? EmailAddress { get; set; }
    
    /// <summary>
    /// Дата и время отправки реестра
    /// </summary>
    public DateTime? SentAt { get; set; }
    
    /// <summary>
    /// Статус отправки: pending, sent, failed
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";
    
    /// <summary>
    /// Сообщение об ошибке (если была)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Количество платежей в реестре
    /// </summary>
    public int PaymentCount { get; set; } = 0;
    
    /// <summary>
    /// Общая сумма платежей в реестре
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; } = 0;
    
    /// <summary>
    /// Содержимое реестра (TAB-разделенные значения)
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    /// Временные метки
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

