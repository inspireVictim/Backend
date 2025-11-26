namespace YessBackend.Application.Services;

/// <summary>
/// Интерфейс сервиса для ежедневной сверки платежей с Optima Bank
/// </summary>
public interface IReconciliationService
{
    /// <summary>
    /// Генерирует реестр платежей за указанную дату
    /// </summary>
    /// <param name="reportDate">Дата, за которую генерируется реестр</param>
    /// <returns>ID созданного реестра</returns>
    Task<int> GenerateReportAsync(DateTime reportDate);
    
    /// <summary>
    /// Отправляет реестр на указанный email адрес
    /// </summary>
    /// <param name="reportId">ID реестра</param>
    /// <param name="emailAddress">Email адрес получателя</param>
    /// <returns>True если отправка успешна</returns>
    Task<bool> SendReportAsync(int reportId, string emailAddress);
    
    /// <summary>
    /// Генерирует и отправляет реестр за указанную дату
    /// </summary>
    /// <param name="reportDate">Дата, за которую генерируется реестр</param>
    /// <param name="emailAddress">Email адрес получателя</param>
    /// <returns>ID созданного реестра</returns>
    Task<int> GenerateAndSendReportAsync(DateTime reportDate, string emailAddress);
    
    /// <summary>
    /// Получает список реестров
    /// </summary>
    /// <param name="startDate">Начальная дата (опционально)</param>
    /// <param name="endDate">Конечная дата (опционально)</param>
    /// <param name="status">Статус реестра (опционально)</param>
    /// <returns>Список реестров</returns>
    Task<List<object>> GetReportsAsync(DateTime? startDate = null, DateTime? endDate = null, string? status = null);
    
    /// <summary>
    /// Получает реестр по ID
    /// </summary>
    /// <param name="reportId">ID реестра</param>
    /// <returns>Данные реестра</returns>
    Task<object?> GetReportByIdAsync(int reportId);
}

