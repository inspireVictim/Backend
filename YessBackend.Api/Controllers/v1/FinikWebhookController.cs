using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using YessBackend.Application.DTOs.FinikPayment;
using YessBackend.Application.Services;

namespace YessBackend.Api.Controllers.v1;

/// <summary>
/// Контроллер для обработки webhook от Finik Payments
/// </summary>
[ApiController]
[Route("finik")]
[Tags("Finik Webhook")]
public class FinikWebhookController : ControllerBase
{
    private readonly IFinikService _finikService;
    private readonly ILogger<FinikWebhookController> _logger;

    public FinikWebhookController(
        IFinikService finikService,
        ILogger<FinikWebhookController> logger)
    {
        _finikService = finikService;
        _logger = logger;
    }

    /// <summary>
    /// Webhook от Finik для уведомлений о статусе платежа
    /// POST /finik/webhook
    /// </summary>
    [HttpPost("webhook")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Webhook()
    {
        try
        {
            // Включаем буферизацию для возможности повторного чтения Request.Body
            Request.EnableBuffering();

            // Читаем raw body для проверки подписи
            Request.Body.Position = 0;
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            // Сбрасываем позицию для повторного чтения
            Request.Body.Position = 0;

            // Парсим webhook
            var webhook = JsonSerializer.Deserialize<FinikWebhookDto>(rawBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (webhook == null)
            {
                _logger.LogWarning("Failed to deserialize Finik webhook");
                return BadRequest(new { error = "Invalid webhook payload" });
            }

            _logger.LogInformation(
                "Received Finik webhook: TransactionId={TransactionId}, Status={Status}",
                webhook.TransactionId, webhook.Status);

            // Читаем все HTTP headers
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in Request.Headers)
            {
                headers[header.Key] = header.Value.ToString();
            }

            // Читаем header signature
            if (!Request.Headers.TryGetValue("signature", out var signatureHeader))
            {
                _logger.LogWarning("Finik webhook missing signature header");
                return Unauthorized(new { error = "Missing signature header" });
            }

            var signature = signatureHeader.ToString();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Finik webhook empty signature header");
                return Unauthorized(new { error = "Empty signature header" });
            }

            // Собираем query-параметры
            var queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var queryParam in Request.Query)
            {
                queryParams[queryParam.Key] = queryParam.Value.ToString();
            }

            // Получаем метод и путь
            var method = Request.Method;
            var absolutePath = Request.Path.Value ?? "/";

            // Сортируем JSON body по ключам для проверки подписи
            string sortedJsonBody;
            try
            {
                var jsonDoc = JsonDocument.Parse(rawBody);
                sortedJsonBody = SortJsonObject(jsonDoc.RootElement);
            }
            catch (JsonException)
            {
                sortedJsonBody = rawBody;
            }

            // Проверяем RSA подпись
            var isValid = _finikService.VerifyWebhookSignature(
                method,
                absolutePath,
                headers,
                queryParams,
                sortedJsonBody,
                signature);

            _logger.LogInformation(
                "Finik webhook signature verification: Method={Method}, Path={Path}, IsValid={IsValid}",
                method, absolutePath, isValid);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Invalid Finik webhook signature for TransactionId={TransactionId}",
                    webhook.TransactionId);
                return Unauthorized(new { error = "Invalid signature" });
            }

            // Обрабатываем webhook
            var success = await _finikService.ProcessWebhookAsync(webhook);

            if (success)
            {
                _logger.LogInformation(
                    "Finik webhook processed successfully: TransactionId={TransactionId}, Status={Status}",
                    webhook.TransactionId, webhook.Status);
                return Ok(new { status = "ok", message = "Webhook processed successfully" });
            }
            else
            {
                _logger.LogWarning(
                    "Failed to process Finik webhook: TransactionId={TransactionId}",
                    webhook.TransactionId);
                return BadRequest(new { error = "Failed to process webhook" });
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Finik webhook JSON");
            return BadRequest(new { error = "Invalid JSON payload" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ошибка обработки Finik webhook: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки Finik webhook");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Сортирует JSON объект по ключам
    /// </summary>
    private string SortJsonObject(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var sortedProperties = element.EnumerateObject()
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p => $"\"{p.Name}\":{SortJsonObject(p.Value)}");

            return "{" + string.Join(",", sortedProperties) + "}";
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var sortedItems = element.EnumerateArray()
                .Select(SortJsonObject);

            return "[" + string.Join(",", sortedItems) + "]";
        }
        else
        {
            return element.GetRawText();
        }
    }
}

