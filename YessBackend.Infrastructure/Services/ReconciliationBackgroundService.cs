using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YessBackend.Application.Services;

namespace YessBackend.Infrastructure.Services;

/// <summary>
/// Background Service для ежедневной генерации и отправки реестров сверки
/// Генерирует реестр за предыдущий день до 10:00 по московскому времени
/// </summary>
public class ReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReconciliationBackgroundService> _logger;
    private readonly TimeZoneInfo _moscowTimeZone;

    public ReconciliationBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ReconciliationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        
        // Получаем московский часовой пояс
        try
        {
            _moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        }
        catch
        {
            try
            {
                _moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
            }
            catch
            {
                // Если не удалось найти, создаем кастомный
                _moscowTimeZone = TimeZoneInfo.CreateCustomTimeZone("MSK", TimeSpan.FromHours(3), "Moscow Time", "Moscow Time");
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reconciliation Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _moscowTimeZone);
                
                // Проверяем, нужно ли генерировать реестр
                // Генерируем реестр за предыдущий день до 10:00 МСК
                var targetTime = new DateTime(now.Year, now.Month, now.Day, 10, 0, 0);
                
                if (now >= targetTime && now < targetTime.AddMinutes(30))
                {
                    // Генерируем реестр за предыдущий день
                    var reportDate = now.Date.AddDays(-1);
                    
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();
                        var emailAddress = _configuration["OptimaPayment:ReconciliationEmail"] 
                            ?? "reconciliation@yess-loyalty.com";
                        
                        try
                        {
                            _logger.LogInformation("Generating reconciliation report for date: {ReportDate}", reportDate);
                            
                            var reportId = await reconciliationService.GenerateAndSendReportAsync(reportDate, emailAddress);
                            
                            _logger.LogInformation("Reconciliation report generated and sent: ReportId={ReportId}, Date={ReportDate}", 
                                reportId, reportDate);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating reconciliation report for date: {ReportDate}", reportDate);
                        }
                    }
                    
                    // Ждем 1 час, чтобы не генерировать повторно
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                else
                {
                    // Проверяем каждые 5 минут
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Reconciliation Background Service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}

