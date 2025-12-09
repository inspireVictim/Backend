using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YessBackend.Application.Config;
using YessBackend.Application.DTOs.FinikPayment;
using YessBackend.Application.Interfaces.Payments;

namespace YessBackend.Infrastructure.Services;

/// <summary>
/// Сервис для работы с Finik Acquiring API
/// </summary>
public class FinikPaymentService : IFinikPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly FinikPaymentConfig _config;
    private readonly IFinikSignatureService _signatureService;
    private readonly ILogger<FinikPaymentService> _logger;

    // Публичные ключи Finik из документации
    private const string FinikProdPublicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAuF/PUmhMPPidcMxhZBPb
BSGJoSphmCI+h6ru8fG8guAlcPMVlhs+ThTjw2LHABvciwtpj51ebJ4EqhlySPyT
hqSfXI6Jp5dPGJNDguxfocohaz98wvT+WAF86DEglZ8dEsfoumojFUy5sTOBdHEu
g94B4BbrJvjmBa1YIx9Azse4HFlWhzZoYPgyQpArhokeHOHIN2QFzJqeriANO+wV
aUMta2AhRVZHbfyJ36XPhGO6A5FYQWgjzkI65cxZs5LaNFmRx6pjnhjIeVKKgF99
4OoYCzhuR9QmWkPl7tL4Kd68qa/xHLz0Psnuhm0CStWOYUu3J7ZpzRK8GoEXRcr8
tQIDAQAB
-----END PUBLIC KEY-----";

    private const string FinikBetaPublicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwlrlKz/8gLWd1ARWGA/8
o3a3Qy8G+hPifyqiPosiTY6nCHovANMIJXk6DH4qAqqZeLu8pLGxudkPbv8dSyG7
F9PZEAryMPzjoB/9P/F6g0W46K/FHDtwTM3YIVvstbEbL19m8yddv/xCT9JPPJTb
LsSTVZq5zCqvKzpupwlGS3Q3oPyLAYe+ZUn4Bx2J1WQrBu3b08fNaR3E8pAkCK27
JqFnP0eFfa817VCtyVKcFHb5ij/D0eUP519Qr/pgn+gsoG63W4pPHN/pKwQUUiAy
uLSHqL5S2yu1dffyMcMVi9E/Q2HCTcez5OvOllgOtkNYHSv9pnrMRuws3u87+hNT
ZwIDAQAB
-----END PUBLIC KEY-----";

    public FinikPaymentService(
        HttpClient httpClient,
        IOptions<FinikPaymentConfig> config,
        IFinikSignatureService signatureService,
        ILogger<FinikPaymentService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _signatureService = signatureService;
        _logger = logger;

        // Настройка HttpClient
        _httpClient.BaseAddress = new Uri(_config.ApiBaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds);
        
        // HttpClient по умолчанию не следует за redirect автоматически при использовании SendAsync
        // Но для надежности можно настроить HttpClientHandler
    }

    public async Task<FinikCreatePaymentResponseDto> CreatePaymentAsync(FinikCreatePaymentRequestDto request)
    {
        if (!_config.Enabled)
        {
            throw new InvalidOperationException("Finik платежи отключены в конфигурации");
        }

        // Генерируем UUID для PaymentId
        var paymentId = Guid.NewGuid().ToString();

        // Формируем тело запроса согласно спецификации
        var requestBody = new FinikPaymentRequestBody
        {
            Amount = request.Amount,
            CardType = "FINIK_QR",
            PaymentId = paymentId,
            RedirectUrl = request.RedirectUrl ?? _config.RedirectUrl,
            Data = new FinikPaymentData
            {
                AccountId = _config.AccountId,
                MerchantCategoryCode = _config.MerchantCategoryCode,
                NameEn = _config.QrName,
                WebhookUrl = _config.WebhookUrl,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            }
        };

        // Генерируем timestamp (UNIX ms)
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        // Получаем host из BaseAddress
        var host = new Uri(_config.ApiBaseUrl).Host;
        var path = "/v1/payment";

        // Формируем заголовки
        var headers = new Dictionary<string, string>
        {
            { "Host", host },
            { "x-api-key", _config.ApiKey },
            { "x-api-timestamp", timestamp }
        };

        // Генерируем каноническую строку и подпись
        var canonicalString = _signatureService.BuildCanonicalString(
            "POST",
            path,
            headers,
            null,
            requestBody);

        var signature = _signatureService.GenerateSignature(canonicalString, _config.PrivateKeyPem);

        // Сериализуем тело запроса с правильными именами свойств
        // Важно: используем тот же формат, что и в канонической строке
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = null // PascalCase для верхнего уровня (Amount, CardType, etc.)
        };

        var jsonBody = JsonSerializer.Serialize(requestBody, jsonOptions);

        _logger.LogInformation("Отправка запроса на создание платежа Finik. PaymentId: {PaymentId}, Amount: {Amount}",
            paymentId, request.Amount);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        // Добавляем заголовки
        requestMessage.Headers.Add("x-api-key", _config.ApiKey);
        requestMessage.Headers.Add("x-api-timestamp", timestamp);
        requestMessage.Headers.Add("signature", signature);

        // Отключаем автоматическое следование redirect
        var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

        _logger.LogInformation("Ответ от Finik API. Status: {Status}, PaymentId: {PaymentId}",
            response.StatusCode, paymentId);

        // Обрабатываем ответ
        if (response.StatusCode == System.Net.HttpStatusCode.Found || 
            response.StatusCode == System.Net.HttpStatusCode.Redirect ||
            response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
            response.StatusCode == System.Net.HttpStatusCode.SeeOther ||
            response.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect)
        {
            // Читаем Location header из 302 ответа
            var location = response.Headers.Location?.ToString();
            
            if (string.IsNullOrEmpty(location))
            {
                var locationHeader = response.Headers.GetValues("Location").FirstOrDefault();
                location = locationHeader;
            }

            if (string.IsNullOrEmpty(location))
            {
                _logger.LogError("Не удалось получить Location header из ответа Finik. PaymentId: {PaymentId}",
                    paymentId);
                throw new InvalidOperationException("Finik API не вернул Location header");
            }

            _logger.LogInformation("Получен payment URL от Finik. PaymentId: {PaymentId}, URL: {Url}",
                paymentId, location);

            return new FinikCreatePaymentResponseDto
            {
                PaymentId = paymentId,
                PaymentUrl = location
            };
        }
        else if (response.IsSuccessStatusCode)
        {
            // Если API вернул 201 с JSON (будущее поведение)
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Finik API вернул успешный ответ: {Content}", content);
            
            // Пытаемся распарсить JSON ответ
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
                var paymentUrl = jsonResponse.TryGetProperty("paymentUrl", out var urlProp) 
                    ? urlProp.GetString() 
                    : null;
                
                if (string.IsNullOrEmpty(paymentUrl))
                {
                    paymentUrl = jsonResponse.TryGetProperty("payment_url", out var urlProp2) 
                        ? urlProp2.GetString() 
                        : null;
                }

                return new FinikCreatePaymentResponseDto
                {
                    PaymentId = paymentId,
                    PaymentUrl = paymentUrl ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось распарсить JSON ответ от Finik API");
                // Если не удалось распарсить, возвращаем с пустым URL
                return new FinikCreatePaymentResponseDto
                {
                    PaymentId = paymentId,
                    PaymentUrl = string.Empty
                };
            }
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка при создании платежа Finik. Status: {Status}, Response: {Response}, PaymentId: {PaymentId}",
                response.StatusCode, errorContent, paymentId);
            
            throw new HttpRequestException($"Finik API вернул ошибку: {response.StatusCode}. {errorContent}");
        }
    }

    public async Task<FinikWebhookDto> GetPaymentStatusAsync(string paymentId)
    {
        // Этот метод не используется в текущей спецификации, но оставлен для совместимости
        throw new NotImplementedException("Получение статуса платежа через API не поддерживается. Используйте webhook.");
    }

    public async Task<bool> ProcessWebhookAsync(FinikWebhookDto webhook)
    {
        // Обработка webhook должна выполняться в контроллере с проверкой подписи
        // Этот метод оставлен для совместимости
        _logger.LogInformation("Обработка webhook Finik. TransactionId: {TransactionId}, Status: {Status}",
            webhook.TransactionId, webhook.Status);
        return true;
    }


    /// <summary>
    /// Получает публичный ключ Finik для проверки подписи
    /// </summary>
    public string GetFinikPublicKey()
    {
        if (!string.IsNullOrEmpty(_config.FinikPublicKeyPem))
        {
            return _config.FinikPublicKeyPem;
        }

        return _config.Environment.ToLowerInvariant() == "beta" 
            ? FinikBetaPublicKey 
            : FinikProdPublicKey;
    }
}
