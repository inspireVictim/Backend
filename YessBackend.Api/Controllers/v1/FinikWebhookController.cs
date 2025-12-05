using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using YessBackend.Application.DTOs.FinikPayment;
using YessBackend.Application.Services;

namespace YessBackend.Api.Controllers.v1;

/// <summary>
/// Контроллер для обработки webhook от Finik
/// </summary>
[ApiController]
[Route("api/v1/finik")]
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
    /// POST /api/v1/finik/webhook
    /// </summary>
    [HttpPost("webhook")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Webhook(
        [FromHeader(Name = "X-Signature")] string? signature)
    {
        try
        {
            // Читаем raw body для проверки подписи
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
            }

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

            _logger.LogInformation("Received Finik webhook: PaymentId={PaymentId}, Status={Status}, OrderId={OrderId}", 
                webhook.PaymentId, webhook.Status, webhook.OrderId);

            // Проверяем подпись если она передана
            if (!string.IsNullOrEmpty(signature))
            {
                if (!_finikService.VerifyWebhookSignature(rawBody, signature))
                {
                    _logger.LogWarning("Invalid Finik webhook signature for PaymentId={PaymentId}", webhook.PaymentId);
                    return Unauthorized(new { error = "Invalid signature" });
                }
            }
            else if (!string.IsNullOrEmpty(webhook.Signature))
            {
                // Проверяем подпись из тела запроса, если она там есть
                if (!_finikService.VerifyWebhookSignature(rawBody, webhook.Signature))
                {
                    _logger.LogWarning("Invalid Finik webhook signature in payload for PaymentId={PaymentId}", webhook.PaymentId);
                    return Unauthorized(new { error = "Invalid signature" });
                }
            }

            // Обрабатываем webhook
            var success = await _finikService.ProcessWebhookAsync(webhook);
            
            if (success)
            {
                return Ok(new { status = "ok", message = "Webhook processed successfully" });
            }
            else
            {
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
            _logger.LogWarning(ex, "Ошибка обработки Finik webhook");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки Finik webhook");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }
}

