using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YessBackend.Application.Services;

namespace YessBackend.Infrastructure.Services;

/// <summary>
/// Сервис для отправки email
/// Поддерживает SendGrid и простую SMTP отправку
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _httpClient;

    public EmailService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<bool> SendEmailAsync(
        string to, 
        string subject, 
        string body, 
        bool isHtml = false,
        string? attachmentContent = null,
        string? attachmentFileName = null)
    {
        try
        {
            var emailEnabled = _configuration.GetValue<bool>("Notifications:EmailEnabled", false);
            var sendGridApiKey = _configuration["Notifications:SendGridApiKey"];
            var fromEmail = _configuration["Notifications:FromEmail"] ?? "noreply@yess-loyalty.com";

            if (!emailEnabled || string.IsNullOrEmpty(sendGridApiKey))
            {
                // Мок-режим: логируем отправку
                _logger.LogInformation(
                    "Mock Email sent: To={To}, Subject={Subject}, HasAttachment={HasAttachment}",
                    to, subject, !string.IsNullOrEmpty(attachmentContent));
                
                // В мок-режиме считаем отправку успешной
                return true;
            }

            // Реальная отправка через SendGrid API
            return await SendViaSendGridAsync(to, subject, body, isHtml, fromEmail, attachmentContent, attachmentFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки email: To={To}, Subject={Subject}", to, subject);
            return false;
        }
    }

    private async Task<bool> SendViaSendGridAsync(
        string to,
        string subject,
        string body,
        bool isHtml,
        string fromEmail,
        string? attachmentContent,
        string? attachmentFileName)
    {
        try
        {
            var sendGridApiKey = _configuration["Notifications:SendGridApiKey"];
            var apiUrl = "https://api.sendgrid.com/v3/mail/send";

            var requestBody = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = to } }
                    }
                },
                from = new { email = fromEmail },
                subject = subject,
                content = new[]
                {
                    new
                    {
                        type = isHtml ? "text/html" : "text/plain",
                        value = body
                    }
                },
                attachments = !string.IsNullOrEmpty(attachmentContent) && !string.IsNullOrEmpty(attachmentFileName)
                    ? new[]
                    {
                        new
                        {
                            content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(attachmentContent)),
                            filename = attachmentFileName,
                            type = "text/plain",
                            disposition = "attachment"
                        }
                    }
                    : null
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {sendGridApiKey}");

            var response = await _httpClient.PostAsync(apiUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully: To={To}, Subject={Subject}", to, subject);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("SendGrid API error: Status={Status}, Error={Error}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки через SendGrid");
            return false;
        }
    }
}

