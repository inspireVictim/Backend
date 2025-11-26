namespace YessBackend.Application.Services;

/// <summary>
/// Интерфейс сервиса для отправки email
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Отправляет email
    /// </summary>
    /// <param name="to">Email получателя</param>
    /// <param name="subject">Тема письма</param>
    /// <param name="body">Тело письма (может быть HTML или plain text)</param>
    /// <param name="isHtml">Является ли тело письма HTML</param>
    /// <param name="attachmentContent">Содержимое вложения (опционально)</param>
    /// <param name="attachmentFileName">Имя файла вложения (опционально)</param>
    /// <returns>True если отправка успешна, иначе false</returns>
    Task<bool> SendEmailAsync(
        string to, 
        string subject, 
        string body, 
        bool isHtml = false,
        string? attachmentContent = null,
        string? attachmentFileName = null);
}

