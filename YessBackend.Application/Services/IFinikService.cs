using YessBackend.Application.DTOs.FinikPayment;

namespace YessBackend.Application.Services;

/// <summary>
/// Интерфейс сервиса для работы с платежным провайдером Finik
/// </summary>
public interface IFinikService
{
    /// <summary>
    /// Создать платеж через Finik API
    /// </summary>
    /// <param name="orderId">ID заказа</param>
    /// <param name="amount">Сумма платежа</param>
    /// <param name="description">Описание платежа</param>
    /// <param name="successUrl">URL редиректа после успешной оплаты</param>
    /// <param name="cancelUrl">URL редиректа после отмены</param>
    /// <returns>Ответ с payment_id и payment_url</returns>
    Task<FinikPaymentResponseDto> CreatePaymentAsync(
        int orderId,
        decimal amount,
        string? description = null,
        string? successUrl = null,
        string? cancelUrl = null);

    /// <summary>
    /// Получить статус платежа из Finik API
    /// </summary>
    /// <param name="paymentId">ID платежа в системе Finik</param>
    /// <returns>Информация о платеже</returns>
    Task<FinikWebhookDto> GetPaymentStatusAsync(string paymentId);

    /// <summary>
    /// Проверить подпись webhook от Finik
    /// </summary>
    /// <param name="payload">Тело webhook запроса</param>
    /// <param name="signature">Подпись из заголовка</param>
    /// <returns>True если подпись валидна</returns>
    bool VerifyWebhookSignature(string payload, string signature);

    /// <summary>
    /// Обработать webhook от Finik и обновить статус заказа
    /// </summary>
    /// <param name="webhook">Данные webhook</param>
    /// <returns>True если обработка успешна</returns>
    Task<bool> ProcessWebhookAsync(FinikWebhookDto webhook);
}

