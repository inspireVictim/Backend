using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using YessBackend.Application.Config;
using YessBackend.Application.DTOs.FinikPayment;
using YessBackend.Application.Interfaces.Payments;
using YessBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace YessBackend.Api.Controllers.v1;

/// <summary>
/// Контроллер для обработки webhook от Finik Acquiring API
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
[Tags("Webhooks")]
public class FinikWebhookController : ControllerBase
{
    private readonly IFinikPaymentService _finikPaymentService;
    private readonly IFinikSignatureService _signatureService;
    private readonly FinikPaymentConfig _config;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<FinikWebhookController> _logger;

    public FinikWebhookController(
        IFinikPaymentService finikPaymentService,
        IFinikSignatureService signatureService,
        IOptions<FinikPaymentConfig> config,
        ApplicationDbContext db,
        ILogger<FinikWebhookController> logger)
    {
        _finikPaymentService = finikPaymentService;
        _signatureService = signatureService;
        _config = config.Value;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Обрабатывает webhook от Finik Acquiring API
    /// POST /api/v1/webhooks/finik
    /// </summary>
    [HttpPost("finik")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            // Читаем тело запроса для проверки подписи
            Request.EnableBuffering();
            var bodyStream = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var bodyString = await bodyStream.ReadToEndAsync();
            Request.Body.Position = 0;

            // Парсим JSON body
            FinikWebhookDto? webhook;
            try
            {
                webhook = JsonSerializer.Deserialize<FinikWebhookDto>(bodyString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Не удалось распарсить JSON body webhook Finik");
                return BadRequest(new { error = "Invalid JSON body" });
            }

            if (webhook == null)
            {
                return BadRequest(new { error = "Webhook body is null" });
            }

            // Проверяем подпись, если включена проверка
            if (_config.VerifySignature)
            {
                var signatureHeader = Request.Headers["signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signatureHeader))
                {
                    _logger.LogWarning("Webhook Finik без подписи. TransactionId: {TransactionId}",
                        webhook.TransactionId);
                    return Unauthorized(new { error = "Missing signature header" });
                }

                // Получаем все заголовки для канонизации
                var headers = new Dictionary<string, string>();
                
                // Host header
                var host = Request.Host.Value;
                if (!string.IsNullOrEmpty(host))
                {
                    headers["Host"] = host;
                }

                // Все x-api-* заголовки
                foreach (var header in Request.Headers)
                {
                    if (header.Key.StartsWith("x-api-", StringComparison.OrdinalIgnoreCase))
                    {
                        headers[header.Key] = header.Value.ToString();
                    }
                }

                // Проверяем timestamp
                var timestampHeader = Request.Headers["x-api-timestamp"].FirstOrDefault();
                if (!string.IsNullOrEmpty(timestampHeader))
                {
                    if (long.TryParse(timestampHeader, out var timestamp))
                    {
                        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        var skew = Math.Abs(now - timestamp);
                        
                        if (skew > _config.TimestampSkewMs)
                        {
                            _logger.LogWarning(
                                "Webhook Finik с недопустимым timestamp. TransactionId: {TransactionId}, Skew: {Skew}ms",
                                webhook.TransactionId, skew);
                            return Unauthorized(new { error = "Timestamp skew too large" });
                        }
                    }
                }

                // Строим каноническую строку
                var path = Request.Path.Value ?? "/api/v1/webhooks/finik";
                var canonicalString = _signatureService.BuildCanonicalString(
                    Request.Method,
                    path,
                    headers,
                    null,
                    webhook);

                // Получаем публичный ключ Finik
                var publicKey = _finikPaymentService.GetFinikPublicKey();

                // Проверяем подпись
                var isValid = _signatureService.VerifySignature(
                    canonicalString,
                    signatureHeader,
                    publicKey);

                if (!isValid)
                {
                    _logger.LogWarning("Неверная подпись webhook Finik. TransactionId: {TransactionId}",
                        webhook.TransactionId);
                    return Unauthorized(new { error = "Invalid signature" });
                }

                _logger.LogInformation("Подпись webhook Finik проверена успешно. TransactionId: {TransactionId}",
                    webhook.TransactionId);
            }

            // Обрабатываем webhook идемпотентно (по transactionId)
            if (!string.IsNullOrEmpty(webhook.TransactionId))
            {
                // Здесь можно добавить проверку в БД, что этот transactionId уже обработан
                // Для простоты просто логируем
                _logger.LogInformation(
                    "Обработка webhook Finik. TransactionId: {TransactionId}, Status: {Status}, Amount: {Amount}",
                    webhook.TransactionId, webhook.Status, webhook.Amount);
            }

            // Обрабатываем статусы SUCCEEDED / FAILED
            if (webhook.Status == "SUCCEEDED")
            {
                _logger.LogInformation(
                    "Платеж Finik успешен. TransactionId: {TransactionId}, Amount: {Amount}, Net: {Net}",
                    webhook.TransactionId, webhook.Amount, webhook.Net);

                // TODO: Обновить статус заказа/платежа в БД
                // Здесь можно добавить логику обновления заказа
            }
            else if (webhook.Status == "FAILED")
            {
                _logger.LogWarning(
                    "Платеж Finik провален. TransactionId: {TransactionId}, Amount: {Amount}",
                    webhook.TransactionId, webhook.Amount);

                // TODO: Обновить статус заказа/платежа в БД
            }

            // Возвращаем 200 OK как можно быстрее
            // Тяжелая обработка должна выполняться асинхронно
            return Ok(new { success = true, transactionId = webhook.TransactionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке webhook Finik");
            // Все равно возвращаем 200, чтобы Finik не повторял запрос
            return Ok(new { success = false, error = "Internal error" });
        }
    }
}
