using System.Text.Json.Serialization;

namespace YessBackend.Application.DTOs.FinikPayment;

/// <summary>
/// Тело запроса для создания платежа в Finik API
/// </summary>
public class FinikPaymentRequestBody
{
    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("CardType")]
    public string CardType { get; set; } = "FINIK_QR";

    [JsonPropertyName("PaymentId")]
    public string PaymentId { get; set; } = string.Empty;

    [JsonPropertyName("RedirectUrl")]
    public string RedirectUrl { get; set; } = string.Empty;

    [JsonPropertyName("Data")]
    public FinikPaymentData Data { get; set; } = new();
}

/// <summary>
/// Данные для QR кода
/// </summary>
public class FinikPaymentData
{
    [JsonPropertyName("accountId")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("merchantCategoryCode")]
    public string MerchantCategoryCode { get; set; } = string.Empty;

    [JsonPropertyName("name_en")]
    public string NameEn { get; set; } = string.Empty;

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("startDate")]
    public long? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public long? EndDate { get; set; }
}

