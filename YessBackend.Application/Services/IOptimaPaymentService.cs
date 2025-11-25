using YessBackend.Application.DTOs.OptimaPayment;

namespace YessBackend.Application.Services;

/// <summary>
/// Интерфейс сервиса для обработки платежей от Optima Bank
/// </summary>
public interface IOptimaPaymentService
{
    /// <summary>
    /// Проверка состояния счета абонента (команда "check")
    /// </summary>
    /// <param name="account">Идентификатор абонента</param>
    /// <param name="txnId">Уникальный идентификатор транзакции (QID)</param>
    /// <param name="sum">Сумма платежа</param>
    /// <param name="ipAddress">IP-адрес запрашивающей стороны (для логирования)</param>
    /// <param name="userAgent">User-Agent запроса (для логирования)</param>
    /// <param name="rawRequest">Raw запрос (для сохранения в БД для сверки)</param>
    Task<OptimaPaymentResponseDto> CheckAccountAsync(
        int account, 
        string txnId, 
        decimal sum,
        string? ipAddress = null,
        string? userAgent = null,
        string? rawRequest = null);
    
    /// <summary>
    /// Пополнение баланса абонента (команда "pay")
    /// </summary>
    /// <param name="account">Идентификатор абонента</param>
    /// <param name="txnId">Уникальный идентификатор транзакции (QID)</param>
    /// <param name="sum">Сумма платежа</param>
    /// <param name="txnDate">Дата и время транзакции</param>
    /// <param name="ipAddress">IP-адрес запрашивающей стороны (для логирования)</param>
    /// <param name="userAgent">User-Agent запроса (для логирования)</param>
    /// <param name="rawRequest">Raw запрос (для сохранения в БД для сверки)</param>
    Task<OptimaPaymentResponseDto> ProcessPaymentAsync(
        int account, 
        string txnId, 
        decimal sum, 
        DateTime txnDate,
        string? ipAddress = null,
        string? userAgent = null,
        string? rawRequest = null);
}

