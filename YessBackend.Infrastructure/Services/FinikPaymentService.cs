using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YessBackend.Application.Config;
using YessBackend.Application.DTOs.FinikPayment;
using YessBackend.Application.Interfaces.Payments;

namespace YessBackend.Infrastructure.Services
{
    public class FinikPaymentService : IFinikPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly FinikPaymentConfig _config;
        private readonly IFinikSignatureService _signatureService;
        private readonly ILogger<FinikPaymentService> _logger;

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

            _httpClient.BaseAddress = new Uri(_config.ApiBaseUrl);
        }

        public async Task<FinikCreatePaymentResponseDto> CreatePaymentAsync(
            FinikCreatePaymentRequestDto request)
        {
            string paymentId = Guid.NewGuid().ToString();

            long startTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long endTs = startTs + 86400000;

            var bodyObject = new
            {
                Amount = request.Amount,
                CardType = "FINIK_QR",
                PaymentId = paymentId,
                RedirectUrl = request.RedirectUrl ?? _config.RedirectUrl,
                Data = new
                {
                    accountId = _config.AccountId,
                    merchantCategoryCode = _config.MerchantCategoryCode,
                    name_en = _config.QrName,
                    webhookUrl = _config.WebhookUrl,
                    description = request.Description,
                    startDate = startTs,
                    endDate = endTs
                }
            };

            // 🔥 ВАЖНО: сериализуем ОДИН РАЗ
            string jsonBody = JsonSerializer.Serialize(bodyObject);
            _logger.LogWarning("JSON BODY:\n{J}", jsonBody);

            string timestamp = DateTimeOffset.UtcNow
                .ToUnixTimeMilliseconds()
                .ToString();

            string host = new Uri(_config.ApiBaseUrl).Host;

            // 🔥 В canonical ТОЛЬКО host + x-api-timestamp
            var canonicalHeaders = new Dictionary<string, string>
            {
                ["host"] = host,
                ["x-api-timestamp"] = timestamp
            };

            string canonical = _signatureService.BuildCanonicalString(
                httpMethod: "POST",
                path: "/v1/payment",
                headers: canonicalHeaders,
                queryParameters: null,
                body: jsonBody        // ⬅️ ПЕРЕДАЁМ СТРОКУ
            );

            _logger.LogWarning("CANONICAL:\n{C}", canonical);

            string privateKey = Environment.GetEnvironmentVariable(
                "FINIK_PRIVATE_KEY")
                ?? throw new Exception("FINIK_PRIVATE_KEY missing");

            string signature = _signatureService.GenerateSignature(
                canonical,
                privateKey
            );

            _logger.LogWarning("SIGNATURE:\n{S}", signature);

            var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "/v1/payment"
            )
            {
                Content = new StringContent(
                    jsonBody,
                    Encoding.UTF8,
                    "application/json"
                )
            };

            // 🔥 В HTTP headers x-api-key ОБЯЗАТЕЛЕН
            httpRequest.Headers.Add("x-api-key", _config.ApiKey);
            httpRequest.Headers.Add("x-api-timestamp", timestamp);
            httpRequest.Headers.Add("signature", signature);

            var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead
            );

            if ((int)response.StatusCode == 302)
            {
                return new FinikCreatePaymentResponseDto
                {
                    PaymentId = paymentId,
                    PaymentUrl = response.Headers.Location!.ToString()
                };
            }

            string error = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Finik ERROR: {Status} {Body}",
                response.StatusCode,
                error
            );

            throw new Exception(
                $"Finik error: {response.StatusCode} {error}"
            );
        }

        public Task<bool> ProcessWebhookAsync(FinikWebhookDto webhook)
        {
            _logger.LogInformation(
                "Finik webhook received: {Status}",
                webhook.Status
            );
            return Task.FromResult(true);
        }
    }
}
