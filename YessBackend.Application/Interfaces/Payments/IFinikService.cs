using YessBackend.Application.DTOs.FinikPayment;

namespace YessBackend.Application.Services;

public interface IFinikService
{
    Task<FinikPaymentResponseDto> CreatePaymentAsync(
        int orderId,
        decimal amount,
        string? description = null,
        string? successUrl = null,
        string? cancelUrl = null);

    Task<FinikWebhookDto> GetPaymentStatusAsync(string paymentId);

    /// <summary>
    /// Проверяет RSA подпись webhook от Finik
    /// </summary>
    bool VerifyWebhookSignature(
        string method,
        string absolutePath,
        Dictionary<string, string> headers,
        Dictionary<string, string> queryParams,
        string jsonBody,
        string signature);

    Task<bool> ProcessWebhookAsync(FinikWebhookDto webhook);
}
