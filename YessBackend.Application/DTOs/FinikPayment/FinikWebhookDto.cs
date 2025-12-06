using System.Text.Json.Serialization;
using System.Text.Json;

namespace YessBackend.Application.DTOs.FinikPayment;

/// <summary>
/// Модель webhook от Finik Payments
/// </summary>
public class FinikWebhookDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("fields")]
    public JsonElement? Fields { get; set; }

    [JsonPropertyName("item")]
    public JsonElement? Item { get; set; }

    [JsonPropertyName("net")]
    public decimal? Net { get; set; }

    [JsonPropertyName("receiptNumber")]
    public string? ReceiptNumber { get; set; }

    [JsonPropertyName("requestDate")]
    public long? RequestDate { get; set; }

    [JsonPropertyName("service")]
    public JsonElement? Service { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("transactionDate")]
    public long? TransactionDate { get; set; }

    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("transactionType")]
    public string? TransactionType { get; set; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}
