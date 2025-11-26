using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YessBackend.Application.Services;

namespace YessBackend.Api.Controllers.v1;

/// <summary>
/// Admin контроллер для управления реестрами сверки платежей Optima Bank
/// </summary>
[ApiController]
[Route("api/v1/admin/optima/reconciliation")]
[Tags("Admin - Optima Reconciliation")]
[Authorize] // Требуется авторизация
public class AdminReconciliationController : ControllerBase
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ILogger<AdminReconciliationController> _logger;

    public AdminReconciliationController(
        IReconciliationService reconciliationService,
        ILogger<AdminReconciliationController> logger)
    {
        _reconciliationService = reconciliationService;
        _logger = logger;
    }

    /// <summary>
    /// Генерирует реестр платежей за указанную дату
    /// </summary>
    /// <param name="date">Дата в формате yyyy-MM-dd (по умолчанию - вчерашний день)</param>
    /// <returns>ID созданного реестра</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateReport([FromQuery] DateTime? date = null)
    {
        try
        {
            var reportDate = date?.Date ?? DateTime.UtcNow.Date.AddDays(-1);
            
            _logger.LogInformation("Manual reconciliation report generation requested: Date={ReportDate}", reportDate);
            
            var reportId = await _reconciliationService.GenerateReportAsync(reportDate);
            
            return Ok(new
            {
                message = "Reconciliation report generated successfully",
                report_id = reportId,
                report_date = reportDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reconciliation report");
            return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
        }
    }

    /// <summary>
    /// Отправляет реестр на указанный email адрес
    /// </summary>
    /// <param name="reportId">ID реестра</param>
    /// <param name="email">Email адрес получателя (опционально, если не указан - используется из конфигурации)</param>
    /// <returns>Результат отправки</returns>
    [HttpPost("{reportId}/send")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendReport(int reportId, [FromQuery] string? email = null)
    {
        try
        {
            var emailAddress = email ?? "reconciliation@yess-loyalty.com";
            
            _logger.LogInformation("Manual reconciliation report send requested: ReportId={ReportId}, Email={Email}", 
                reportId, emailAddress);
            
            var success = await _reconciliationService.SendReportAsync(reportId, emailAddress);
            
            if (success)
            {
                return Ok(new
                {
                    message = "Reconciliation report sent successfully",
                    report_id = reportId,
                    email = emailAddress
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    error = "Failed to send reconciliation report",
                    report_id = reportId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reconciliation report: ReportId={ReportId}", reportId);
            return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
        }
    }

    /// <summary>
    /// Генерирует и отправляет реестр за указанную дату
    /// </summary>
    /// <param name="date">Дата в формате yyyy-MM-dd (по умолчанию - вчерашний день)</param>
    /// <param name="email">Email адрес получателя (опционально)</param>
    /// <returns>ID созданного реестра</returns>
    [HttpPost("generate-and-send")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateAndSendReport([FromQuery] DateTime? date = null, [FromQuery] string? email = null)
    {
        try
        {
            var reportDate = date?.Date ?? DateTime.UtcNow.Date.AddDays(-1);
            var emailAddress = email ?? "reconciliation@yess-loyalty.com";
            
            _logger.LogInformation("Manual reconciliation report generate-and-send requested: Date={ReportDate}, Email={Email}", 
                reportDate, emailAddress);
            
            var reportId = await _reconciliationService.GenerateAndSendReportAsync(reportDate, emailAddress);
            
            return Ok(new
            {
                message = "Reconciliation report generated and sent successfully",
                report_id = reportId,
                report_date = reportDate,
                email = emailAddress
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating and sending reconciliation report");
            return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
        }
    }

    /// <summary>
    /// Получает список реестров
    /// </summary>
    /// <param name="startDate">Начальная дата (опционально)</param>
    /// <param name="endDate">Конечная дата (опционально)</param>
    /// <param name="status">Статус реестра: pending, sent, failed (опционально)</param>
    /// <returns>Список реестров</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var reports = await _reconciliationService.GetReportsAsync(startDate, endDate, status);
            
            return Ok(new
            {
                reports = reports,
                count = reports.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reconciliation reports");
            return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
        }
    }

    /// <summary>
    /// Получает реестр по ID
    /// </summary>
    /// <param name="reportId">ID реестра</param>
    /// <returns>Данные реестра</returns>
    [HttpGet("{reportId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportById(int reportId)
    {
        try
        {
            var report = await _reconciliationService.GetReportByIdAsync(reportId);
            
            if (report == null)
            {
                return NotFound(new { error = "Reconciliation report not found", report_id = reportId });
            }
            
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reconciliation report: ReportId={ReportId}", reportId);
            return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
        }
    }
}

