using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YessBackend.Application.DTOs.FinikPayment;
using YessBackend.Application.Interfaces.Payments;

namespace YessBackend.Api.Controllers.v1;

/// <summary>
/// Контроллер для работы с платежами
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Tags("Payments")]
public class PaymentsController : ControllerBase
{
    private readonly IFinikPaymentService _finikPaymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IFinikPaymentService finikPaymentService,
        ILogger<PaymentsController> logger)
    {
        _finikPaymentService = finikPaymentService;
        _logger = logger;
    }

    /// <summary>
    /// Создает платеж через Finik Acquiring API
    /// POST /api/v1/payments/create
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(FinikCreatePaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FinikCreatePaymentResponseDto>> CreatePayment(
        [FromBody] FinikCreatePaymentRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Создание платежа Finik. Amount: {Amount}, Description: {Description}",
                request.Amount, request.Description);

            var result = await _finikPaymentService.CreatePaymentAsync(request);

            _logger.LogInformation("Платеж Finik создан успешно. PaymentId: {PaymentId}",
                result.PaymentId);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ошибка при создании платежа Finik");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Внутренняя ошибка при создании платежа Finik");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }
}
