using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YessBackend.Application.DTOs.FinikPayment;
using YessBackend.Application.Services;
using YessBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using YessBackend.Domain.Entities;

namespace YessBackend.Infrastructure.Services;

/// <summary>
/// Сервис для работы с Finik Payments API и обработки webhook
/// </summary>
public class FinikService : IFinikService
{
    private const string FINIK_PUBLIC_KEY = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAuF/PUmhMPPidcMxhZBPb
BSGJoSphmCI+h6ru8fG8guAlcPMVlhs+ThTjw2LHABvciwtpj51ebJ4EqhlySPyT
hqSfXI6Jp5dPGJNDguxfocohaz98wvT+WAF86DEglZ8dEsfoumojFUy5sTOBdHEu
g94B4BbrJvjmBa1YIx9Azse4HFlWhzZoYPgyQpArhokeHOHIN2QFzJqeriANO+wV
aUMta2AhRVZHbfyJ36XPhGO6A5FYQWgjzkI65cxZs5LaNFmRx6pjnhjIeVKKgF99
4OoYCzhuR9QmWkPl7tL4Kd68qa/xHLz0Psnuhm0CStWOYUu3J7ZpzRK8GoEXRcr8
tQIDAQAB
-----END PUBLIC KEY-----";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<FinikService> _logger;
    private readonly RSA _rsaPublicKey;

    public FinikService(
        ApplicationDbContext context,
        ILogger<FinikService> logger)
    {
        _context = context;
        _logger = logger;
        _rsaPublicKey = ImportPublicKey(FINIK_PUBLIC_KEY);
    }

    public Task<FinikPaymentResponseDto> CreatePaymentAsync(
        int orderId,
        decimal amount,
        string? description = null,
        string? successUrl = null,
        string? cancelUrl = null)
    {
        // Реализация создания платежа через Finik API
        // Пока не требуется для webhook
        throw new NotImplementedException("Use IFinikPaymentService for creating payments");
    }

    public Task<FinikWebhookDto> GetPaymentStatusAsync(string paymentId)
    {
        // Реализация получения статуса платежа
        // Пока не требуется для webhook
        throw new NotImplementedException("Use IFinikPaymentService for getting payment status");
    }

    /// <summary>
    /// Проверяет RSA подпись webhook от Finik
    /// </summary>
    public bool VerifyWebhookSignature(
        string method,
        string absolutePath,
        Dictionary<string, string> headers,
        Dictionary<string, string> queryParams,
        string jsonBody,
        string signature)
    {
        try
        {
            // Собираем строку data по алгоритму Finik
            var dataString = BuildDataString(method, absolutePath, headers, queryParams, jsonBody);

            _logger.LogInformation("Finik webhook data string: {DataString}", dataString);

            // Декодируем Base64 подпись
            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromBase64String(signature);
            }
            catch (FormatException)
            {
                _logger.LogWarning("Invalid Base64 signature format");
                return false;
            }

            // Вычисляем SHA256 хеш от data
            byte[] dataHash;
            using (var sha256 = SHA256.Create())
            {
                dataHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataString));
            }

            // Проверяем RSA подпись
            var rsaDeformatter = new RSAPKCS1SignatureDeformatter(_rsaPublicKey);
            rsaDeformatter.SetHashAlgorithm("SHA256");

            var isValid = rsaDeformatter.VerifySignature(dataHash, signatureBytes);

            _logger.LogInformation("Finik webhook signature verification: {IsValid}", isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Finik webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Обрабатывает webhook от Finik
    /// </summary>
    public async Task<bool> ProcessWebhookAsync(FinikWebhookDto webhook)
    {
        try
        {
            _logger.LogInformation(
                "Processing Finik webhook: TransactionId={TransactionId}, Status={Status}",
                webhook.TransactionId, webhook.Status);

            if (string.IsNullOrEmpty(webhook.TransactionId))
            {
                _logger.LogWarning("Finik webhook missing transactionId");
                return false;
            }

            // Извлекаем order ID из external_id в fields или data
            int? orderId = null;
            
            if (webhook.Fields.HasValue)
            {
                var fields = webhook.Fields.Value;
                if (fields.TryGetProperty("external_id", out var externalId))
                {
                    if (externalId.ValueKind == JsonValueKind.String)
                    {
                        if (int.TryParse(externalId.GetString(), out var parsedOrderId))
                        {
                            orderId = parsedOrderId;
                        }
                    }
                }
            }

            if (!orderId.HasValue && webhook.Data.HasValue)
            {
                var data = webhook.Data.Value;
                if (data.TryGetProperty("external_id", out var externalId))
                {
                    if (externalId.ValueKind == JsonValueKind.String)
                    {
                        if (int.TryParse(externalId.GetString(), out var parsedOrderId))
                        {
                            orderId = parsedOrderId;
                        }
                    }
                }
            }

            if (!orderId.HasValue)
            {
                _logger.LogWarning("Finik webhook missing order ID in external_id");
                return false;
            }

            // Обрабатываем статус платежа
            if (webhook.Status == "SUCCEEDED")
            {
                await MarkPaidAsync(orderId.Value, webhook);
            }
            else if (webhook.Status == "FAILED")
            {
                await MarkFailedAsync(orderId.Value, webhook);
            }
            else
            {
                _logger.LogInformation("Finik webhook status {Status} - no action required", webhook.Status);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Finik webhook");
            return false;
        }
    }

    /// <summary>
    /// Собирает строку data для проверки подписи по алгоритму Finik
    /// </summary>
    private string BuildDataString(
        string method,
        string absolutePath,
        Dictionary<string, string> headers,
        Dictionary<string, string> queryParams,
        string jsonBody)
    {
        var sb = new StringBuilder();

        // 1. Lowercase HTTP method + "\n"
        sb.Append(method.ToLowerInvariant());
        sb.Append("\n");

        // 2. URI Absolute Path + "\n"
        sb.Append(absolutePath);
        sb.Append("\n");

        // 3. Headers: сначала host, потом все x-api-*, отсортированные по имени
        var headerParts = new List<string>();

        // Добавляем host
        if (headers.TryGetValue("host", out var hostValue))
        {
            headerParts.Add($"host:{hostValue}");
        }

        // Добавляем все headers, начинающиеся с x-api-
        var apiHeaders = headers
            .Where(h => h.Key.StartsWith("x-api-", StringComparison.OrdinalIgnoreCase))
            .OrderBy(h => h.Key, StringComparer.OrdinalIgnoreCase)
            .Select(h => $"{h.Key.ToLowerInvariant()}:{h.Value}");

        headerParts.AddRange(apiHeaders);

        if (headerParts.Count > 0)
        {
            sb.Append(string.Join("&", headerParts));
        }

        // 4. Query-параметры: отсортированные по key, формат key=value&key2=value2
        // Если есть query - добавляем "\n" после headers, затем query и "\n"
        // Если нет query - не добавляем "\n" после headers (согласно требованиям)
        if (queryParams.Count > 0)
        {
            // Добавляем "\n" после headers (если они есть) или после path (если headers нет)
            sb.Append("\n");
            var queryString = string.Join("&",
                queryParams
                    .OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(q => $"{q.Key}={q.Value}"));

            sb.Append(queryString);
            sb.Append("\n");
        }
        // Если нет query - не добавляем "\n", сразу идет JSON body

        // 5. JSON body: отсортированный по ключам
        if (!string.IsNullOrEmpty(jsonBody))
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(jsonBody);
                var sortedJson = SortJsonObject(jsonDoc.RootElement);
                sb.Append(sortedJson);
            }
            catch (JsonException)
            {
                // Если не удалось распарсить JSON, используем исходный
                sb.Append(jsonBody);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Сортирует JSON объект по ключам
    /// </summary>
    private string SortJsonObject(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var sortedProperties = element.EnumerateObject()
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p => $"\"{p.Name}\":{SortJsonObject(p.Value)}");

            return "{" + string.Join(",", sortedProperties) + "}";
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var sortedItems = element.EnumerateArray()
                .Select(SortJsonObject);

            return "[" + string.Join(",", sortedItems) + "]";
        }
        else
        {
            return element.GetRawText();
        }
    }

    /// <summary>
    /// Импортирует публичный RSA ключ из PEM формата
    /// </summary>
    private RSA ImportPublicKey(string pemKey)
    {
        var rsa = RSA.Create();

        // Удаляем заголовки и переносы строк
        var keyBase64 = pemKey
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();

        var keyBytes = Convert.FromBase64String(keyBase64);
        rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);

        return rsa;
    }

    /// <summary>
    /// Отмечает заказ как оплаченный
    /// </summary>
    private async Task MarkPaidAsync(int orderId, FinikWebhookDto webhook)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for Finik webhook", orderId);
            return;
        }

        if (order.PaymentStatus == "paid")
        {
            _logger.LogInformation("Order {OrderId} already marked as paid", orderId);
            return;
        }

        // Обновляем статус заказа
        order.Status = OrderStatus.Paid;
        order.PaymentStatus = "paid";
        order.PaymentMethod = "finik";

        // Устанавливаем дату оплаты
        if (webhook.TransactionDate.HasValue)
        {
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(webhook.TransactionDate.Value).DateTime;
            order.PaidAt = timestamp;
        }
        else
        {
            order.PaidAt = DateTime.UtcNow;
        }

        // Создаем или обновляем транзакцию
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.OrderId == orderId && t.GatewayTransactionId == webhook.TransactionId);

        if (transaction == null)
        {
            transaction = new Transaction
            {
                UserId = order.UserId,
                OrderId = orderId,
                PartnerId = order.PartnerId,
                Type = "payment",
                Amount = webhook.Amount ?? order.FinalAmount,
                PaymentMethod = "finik",
                GatewayTransactionId = webhook.TransactionId,
                Status = "completed",
                Description = $"Оплата заказа #{orderId} через Finik",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = order.PaidAt
            };

            _context.Transactions.Add(transaction);
        }
        else
        {
            transaction.Status = "completed";
            transaction.CompletedAt = order.PaidAt;
        }

        order.TransactionId = transaction.Id;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} marked as paid via Finik webhook", orderId);
    }

    /// <summary>
    /// Отмечает заказ как неудачный
    /// </summary>
    private async Task MarkFailedAsync(int orderId, FinikWebhookDto webhook)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for Finik webhook", orderId);
            return;
        }

        // Обновляем статус платежа
        order.PaymentStatus = "failed";

        // Обновляем транзакцию, если есть
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.OrderId == orderId && t.GatewayTransactionId == webhook.TransactionId);

        if (transaction != null)
        {
            transaction.Status = "failed";
            transaction.ErrorMessage = "Payment failed via Finik";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} marked as failed via Finik webhook", orderId);
    }
}

