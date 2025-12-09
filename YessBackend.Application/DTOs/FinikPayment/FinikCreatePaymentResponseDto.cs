namespace YessBackend.Application.DTOs.FinikPayment;

/// <summary>
/// DTO ответа на создание платежа через Finik
/// </summary>
public class FinikCreatePaymentResponseDto
{
    /// <summary>
    /// URL для оплаты (из Location header при 302 redirect)
    /// </summary>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// Уникальный ID платежа (PaymentId)
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;
}

