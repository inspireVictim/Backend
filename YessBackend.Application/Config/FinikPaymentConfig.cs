namespace YessBackend.Application.Config;

/// <summary>
/// Конфигурация для интеграции с Finik Acquiring API
/// </summary>
public class FinikPaymentConfig
{
    /// <summary>
    /// Включен ли прием платежей через Finik
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// API Key от Finik (x-api-key)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Account ID от Finik (accountId в Data объекте)
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Приватный ключ в формате PEM для подписи запросов
    /// </summary>
    public string PrivateKeyPem { get; set; } = string.Empty;

    /// <summary>
    /// Публичный ключ Finik в формате PEM для проверки webhook
    /// Если не указан, используется публичный ключ из документации (prod/beta)
    /// </summary>
    public string? FinikPublicKeyPem { get; set; }

    /// <summary>
    /// Окружение: "production" или "beta"
    /// </summary>
    public string Environment { get; set; } = "production";

    /// <summary>
    /// Базовый URL API Finik
    /// Production: https://api.acquiring.averspay.kg
    /// Beta: https://beta.api.acquiring.averspay.kg
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.acquiring.averspay.kg";

    /// <summary>
    /// URL для webhook от Finik
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL для редиректа после успешной оплаты
    /// </summary>
    public string RedirectUrl { get; set; } = string.Empty;

    /// <summary>
    /// Merchant Category Code (MCC)
    /// </summary>
    public string MerchantCategoryCode { get; set; } = "0742";

    /// <summary>
    /// Название QR кода (name_en)
    /// </summary>
    public string QrName { get; set; } = "Yess Payment";

    /// <summary>
    /// Таймаут запросов к API Finik (в секундах)
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Включена ли проверка подписи webhook
    /// </summary>
    public bool VerifySignature { get; set; } = true;

    /// <summary>
    /// Допустимое отклонение timestamp в миллисекундах (по умолчанию 5 минут)
    /// </summary>
    public long TimestampSkewMs { get; set; } = 300000; // 5 minutes
}

