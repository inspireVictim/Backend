using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YessBackend.Application.DTOs.FinikPayment;
using YessBackend.Application.Services;

namespace YessBackend.Api.Controllers.v1;

/// <summary>
/// Контроллер для работы с платежами через Finik
/// </summary>
[ApiController]
[Route("api/v1/payment")]
[Tags("Finik Payment")]
public class FinikPaymentController : ControllerBase
{
    private readonly IFinikService _finikService;
    private readonly ILogger<FinikPaymentController> _logger;

    public FinikPaymentController(
        IFinikService finikService,
        ILogger<FinikPaymentController> logger)
    {
        _finikService = finikService;
        _logger = logger;
    }

    /// <summary>
    /// Создать платеж через Finik
    /// POST /api/v1/payment/create
    /// </summary>
    [HttpPost("create")]
    [Authorize]
    [ProducesResponseType(typeof(FinikPaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FinikPaymentResponseDto>> CreatePayment([FromBody] FinikPaymentRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Неверный токен" });
            }

            _logger.LogInformation("Creating Finik payment: OrderId={OrderId}, Amount={Amount}, UserId={UserId}", 
                request.OrderId, request.Amount, userId);

            var response = await _finikService.CreatePaymentAsync(
                orderId: request.OrderId,
                amount: request.Amount,
                description: request.Description,
                successUrl: request.SuccessUrl,
                cancelUrl: request.CancelUrl);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ошибка создания платежа Finik");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка создания платежа Finik");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить статус платежа
    /// GET /api/v1/payment/{payment_id}/status
    /// </summary>
    [HttpGet("{payment_id}/status")]
    [Authorize]
    [ProducesResponseType(typeof(FinikWebhookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FinikWebhookDto>> GetPaymentStatus([FromRoute] string payment_id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Неверный токен" });
            }

            var status = await _finikService.GetPaymentStatusAsync(payment_id);
            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ошибка получения статуса платежа Finik");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения статуса платежа Finik");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}

