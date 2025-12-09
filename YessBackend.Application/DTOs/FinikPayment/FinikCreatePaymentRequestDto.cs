using System.ComponentModel.DataAnnotations;

namespace YessBackend.Application.DTOs.FinikPayment;

/// <summary>
/// DTO для создания платежа через Finik
/// </summary>
public class FinikCreatePaymentRequestDto
{
    /// <summary>
    /// Сумма платежа
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Описание платежа
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL для редиректа после успешной оплаты
    /// </summary>
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// ID заказа (опционально, для связи с заказом)
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Начало действия QR кода (timestamp в миллисекундах)
    /// </summary>
    public long? StartDate { get; set; }

    /// <summary>
    /// Конец действия QR кода (timestamp в миллисекундах)
    /// </summary>
    public long? EndDate { get; set; }
}

