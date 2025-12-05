namespace YessBackend.Application.Config;

/// <summary>
/// Конфигурация для интеграции с Finik платежным провайдером
/// </summary>
public class FinikPaymentConfig
{
    /// <summary>
    /// Включен ли прием платежей через Finik
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Client ID от Finik
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret от Finik (хранится в переменных окружения или appsettings.Production.json)
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Account ID от Finik
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Базовый URL API Finik
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.finik.kg";

    /// <summary>
    /// URL для callback/webhook от Finik
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Таймаут запросов к API Finik (в секундах)
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Включена ли проверка подписи webhook
    /// </summary>
    public bool VerifySignature { get; set; } = true;
}

