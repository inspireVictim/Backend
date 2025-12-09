using YessBackend.Application.DTOs.FinikPayment;

namespace YessBackend.Application.Interfaces.Payments;

public interface IFinikPaymentService
{
    /// <summary>
    /// Создает платеж через Finik Acquiring API
    /// </summary>
    Task<FinikCreatePaymentResponseDto> CreatePaymentAsync(FinikCreatePaymentRequestDto request);

    /// <summary>
    /// Получает публичный ключ Finik для проверки подписи
    /// </summary>
    string GetFinikPublicKey();
}
