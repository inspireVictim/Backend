using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace YessBackend.Application.DTOs.FinikPayment;

/// <summary>
/// DTO для запроса создания платежа через Finik
/// </summary>
public class FinikPaymentRequestDto
{
    /// <summary>
    /// ID заказа
    /// </summary>
    [Required]
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    /// <summary>
    /// Сумма платежа
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше 0")]
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Описание платежа
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// URL для редиректа после успешной оплаты
    /// </summary>
    [JsonPropertyName("success_url")]
    public string? SuccessUrl { get; set; }

    /// <summary>
    /// URL для редиректа после отмены оплаты
    /// </summary>
    [JsonPropertyName("cancel_url")]
    public string? CancelUrl { get; set; }
}

/// <summary>
/// DTO для ответа при создании платежа через Finik
/// </summary>
public class FinikPaymentResponseDto
{
    /// <summary>
    /// ID платежа в системе Finik
    /// </summary>
    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// URL для перенаправления пользователя на оплату
    /// </summary>
    [JsonPropertyName("payment_url")]
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// ID заказа
    /// </summary>
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    /// <summary>
    /// Сумма платежа
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Статус платежа
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Сообщение
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// DTO для webhook от Finik
/// </summary>
public class FinikWebhookDto
{
    /// <summary>
    /// ID платежа
    /// </summary>
    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// ID заказа (external_id)
    /// </summary>
    [JsonPropertyName("order_id")]
    public int? OrderId { get; set; }

    /// <summary>
    /// Статус платежа
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Сумма платежа
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Валюта
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Дата создания платежа
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Дата обновления платежа
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Дата оплаты
    /// </summary>
    [JsonPropertyName("paid_at")]
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Подпись запроса
    /// </summary>
    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

