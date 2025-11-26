using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using YessBackend.Application.Services;
using YessBackend.Domain.Entities;
using YessBackend.Infrastructure.Data;

namespace YessBackend.Infrastructure.Services;

/// <summary>
/// Сервис для ежедневной сверки платежей с Optima Bank
/// Реализует требования QIWI OSMP v1.4
/// </summary>
public class ReconciliationService : IReconciliationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReconciliationService> _logger;

    public ReconciliationService(
        ApplicationDbContext context,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ReconciliationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> GenerateReportAsync(DateTime reportDate)
    {
        try
        {
            // Проверяем, не существует ли уже реестр за эту дату
            var existingReport = await _context.ReconciliationReports
                .FirstOrDefaultAsync(r => r.ReportDate.Date == reportDate.Date);

            if (existingReport != null)
            {
                _logger.LogWarning("Reconciliation report already exists for date: {ReportDate}", reportDate.Date);
                return existingReport.Id;
            }

            // Получаем все успешно обработанные платежи за указанную дату
            // Формат даты в txn_date: ГГГГММДДЧЧММСС
            var reportDateStr = reportDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            
            var payments = await _context.PaymentProviderTransactions
                .Where(t => 
                    t.Provider == "optima_bank" &&
                    t.OperationType == "pay" &&
                    t.Status == "success" &&
                    t.IsProcessed &&
                    t.TxnDate != null &&
                    t.TxnDate.StartsWith(reportDateStr))
                .OrderBy(t => t.TxnDate)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Found {Count} payments for reconciliation report date: {ReportDate}", 
                payments.Count, reportDate.Date);

            // Генерируем содержимое реестра согласно формату QIWI OSMP v1.4
            var content = GenerateReconciliationContent(payments, reportDate);

            var report = new ReconciliationReport
            {
                ReportDate = reportDate.Date,
                GeneratedAt = DateTime.UtcNow,
                Status = "pending",
                PaymentCount = payments.Count,
                TotalAmount = payments.Where(p => p.Amount.HasValue).Sum(p => p.Amount!.Value),
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ReconciliationReports.Add(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reconciliation report generated: ReportId={ReportId}, Date={ReportDate}, Payments={Count}, Total={Total}",
                report.Id, reportDate.Date, report.PaymentCount, report.TotalAmount);

            return report.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка генерации реестра сверки: ReportDate={ReportDate}", reportDate);
            throw;
        }
    }

    public async Task<bool> SendReportAsync(int reportId, string emailAddress)
    {
        try
        {
            var report = await _context.ReconciliationReports
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
            {
                _logger.LogWarning("Reconciliation report not found: ReportId={ReportId}", reportId);
                return false;
            }

            if (string.IsNullOrEmpty(report.Content))
            {
                _logger.LogWarning("Reconciliation report content is empty: ReportId={ReportId}", reportId);
                return false;
            }

            // Формируем тему письма
            var subject = $"Реестр платежей Optima Bank за {report.ReportDate:dd.MM.yyyy}";
            
            // Тело письма
            var body = new StringBuilder();
            body.AppendLine($"Реестр платежей за {report.ReportDate:dd.MM.yyyy}");
            body.AppendLine($"Количество платежей: {report.PaymentCount}");
            body.AppendLine($"Общая сумма: {report.TotalAmount:F2} руб.");
            body.AppendLine();
            body.AppendLine("Реестр прикреплен к письму.");

            // Отправляем email с вложением
            var success = await _emailService.SendEmailAsync(
                to: emailAddress,
                subject: subject,
                body: body.ToString(),
                isHtml: false,
                attachmentContent: report.Content,
                attachmentFileName: $"reconciliation_{report.ReportDate:yyyyMMdd}.txt");

            if (success)
            {
                report.EmailAddress = emailAddress;
                report.SentAt = DateTime.UtcNow;
                report.Status = "sent";
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reconciliation report sent: ReportId={ReportId}, Email={Email}", 
                    reportId, emailAddress);
            }
            else
            {
                report.Status = "failed";
                report.ErrorMessage = "Failed to send email";
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Failed to send reconciliation report: ReportId={ReportId}, Email={Email}", 
                    reportId, emailAddress);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки реестра сверки: ReportId={ReportId}, Email={Email}", 
                reportId, emailAddress);
            
            // Обновляем статус на failed
            var report = await _context.ReconciliationReports.FindAsync(reportId);
            if (report != null)
            {
                report.Status = "failed";
                report.ErrorMessage = ex.Message;
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            
            return false;
        }
    }

    public async Task<int> GenerateAndSendReportAsync(DateTime reportDate, string emailAddress)
    {
        var reportId = await GenerateReportAsync(reportDate);
        await SendReportAsync(reportId, emailAddress);
        return reportId;
    }

    public async Task<List<object>> GetReportsAsync(DateTime? startDate = null, DateTime? endDate = null, string? status = null)
    {
        try
        {
            var query = _context.ReconciliationReports.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(r => r.ReportDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.ReportDate <= endDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var reports = await query
                .OrderByDescending(r => r.ReportDate)
                .ThenByDescending(r => r.GeneratedAt)
                .ToListAsync();

            return reports.Select(r => new
            {
                id = r.Id,
                report_date = r.ReportDate,
                generated_at = r.GeneratedAt,
                email_address = r.EmailAddress,
                sent_at = r.SentAt,
                status = r.Status,
                error_message = r.ErrorMessage,
                payment_count = r.PaymentCount,
                total_amount = r.TotalAmount,
                created_at = r.CreatedAt,
                updated_at = r.UpdatedAt
            }).Cast<object>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения списка реестров");
            throw;
        }
    }

    public async Task<object?> GetReportByIdAsync(int reportId)
    {
        try
        {
            var report = await _context.ReconciliationReports
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
            {
                return null;
            }

            return new
            {
                id = report.Id,
                report_date = report.ReportDate,
                generated_at = report.GeneratedAt,
                email_address = report.EmailAddress,
                sent_at = report.SentAt,
                status = report.Status,
                error_message = report.ErrorMessage,
                payment_count = report.PaymentCount,
                total_amount = report.TotalAmount,
                content = report.Content,
                created_at = report.CreatedAt,
                updated_at = report.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения реестра: ReportId={ReportId}", reportId);
            throw;
        }
    }

    /// <summary>
    /// Генерирует содержимое реестра в формате QIWI OSMP v1.4
    /// Формат: TAB-разделенные значения, UTF-8
    /// </summary>
    private string GenerateReconciliationContent(List<PaymentProviderTransaction> payments, DateTime reportDate)
    {
        var sb = new StringBuilder();
        
        // Email адрес получателя (первая строка)
        var emailAddress = _configuration["OptimaPayment:ReconciliationEmail"] ?? "reconciliation@yess-loyalty.com";
        sb.AppendLine(emailAddress);
        
        // Пустая строка
        sb.AppendLine();
        
        // Заголовок (опционально, для удобства чтения)
        // Согласно документации QIWI, заголовок не обязателен
        
        // Данные платежей
        // Формат: <txn_id> <дата> <время> <идентификатор абонента> <сумма>
        // Поля разделены TAB (\t)
        foreach (var payment in payments)
        {
            if (payment.TxnDate == null || payment.Amount == null || string.IsNullOrEmpty(payment.Account))
            {
                continue;
            }

            // Парсим txn_date: ГГГГММДДЧЧММСС
            var txnDate = payment.TxnDate;
            if (txnDate.Length >= 14)
            {
                var dateStr = $"{txnDate.Substring(6, 2)}.{txnDate.Substring(4, 2)}.{txnDate.Substring(0, 4)}";
                var timeStr = $"{txnDate.Substring(8, 2)}:{txnDate.Substring(10, 2)}:{txnDate.Substring(12, 2)}";
                
                // Формат: txn_id, дата, время, account, сумма
                // Разделитель: TAB
                sb.Append($"{payment.Qid}\t{dateStr}\t{timeStr}\t{payment.Account}\t{payment.Amount.Value:F2}");
                sb.AppendLine();
            }
        }
        
        // Итоговая строка
        // Формат: Total: <кол-во платежей> <общая сумма>
        var totalAmount = payments.Where(p => p.Amount.HasValue).Sum(p => p.Amount!.Value);
        sb.AppendLine($"Total:\t{payments.Count}\t{totalAmount:F2}");
        
        return sb.ToString();
    }
}

