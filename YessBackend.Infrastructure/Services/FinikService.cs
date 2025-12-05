using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YessBackend.Application.Config;
using YessBackend.Application.DTOs.FinikPayment;
using YessBackend.Application.Services;
using YessBackend.Domain.Entities;
using YessBackend.Infrastructure.Data;

namespace YessBackend.Infrastructure.Services;

/// <summary>
/// Сервис для работы с платежным провайдером Finik
/// </summary>
public class FinikService : IFinikService
{
    private readonly HttpClient _httpClient;
    private readonly FinikPaymentConfig _config;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FinikService> _logger;

    public FinikService(
        HttpClient httpClient,
        IOptions<FinikPaymentConfig> config,
        ApplicationDbContext context,
        ILogger<FinikService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _context = context;
        _logger = logger;

        // Настройка HttpClient
        _httpClient.BaseAddress = new Uri(_config.ApiBaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds);
    }

    public async Task<FinikPaymentResponseDto> CreatePaymentAsync(
        int orderId,
        decimal amount,
        string? description = null,
        string? successUrl = null,
        string? cancelUrl = null)
    {
        if (!_config.Enabled)
        {
            throw new InvalidOperationException("Finik payment processing is disabled");
        }

        try
        {
            // Проверяем заказ
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                throw new InvalidOperationException($"Order {orderId} not found");
            }

            // Формируем запрос к Finik API
            var requestBody = new
            {
                account_id = _config.AccountId,
                amount = amount,
                currency = "KGS",
                description = description ?? $"Order #{orderId} payment",
                external_id = orderId.ToString(),
                success_url = successUrl ?? $"{_config.CallbackUrl}/success?order_id={orderId}",
                cancel_url = cancelUrl ?? $"{_config.CallbackUrl}/cancel?order_id={orderId}",
                callback_url = _config.CallbackUrl
            };

            _logger.LogInformation("Creating Finik payment for order {OrderId}, amount: {Amount}", orderId, amount);

            // Добавляем авторизацию
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

            // Отправляем запрос
            var response = await _httpClient.PostAsJsonAsync("/api/v1/payments", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Finik API error: {StatusCode}, {Content}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Finik API error: {response.StatusCode}");
            }

            var finikResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            var paymentId = finikResponse.GetProperty("id").GetString() ?? string.Empty;
            var paymentUrl = finikResponse.GetProperty("payment_url").GetString() ?? string.Empty;

            _logger.LogInformation("Finik payment created: PaymentId={PaymentId}, OrderId={OrderId}", paymentId, orderId);

            // Сохраняем транзакцию в PaymentProviderTransaction
            var providerTransaction = new PaymentProviderTransaction
            {
                Qid = paymentId,
                Provider = "finik",
                OperationType = "payment_create",
                Account = order.UserId.ToString(),
                Amount = amount,
                Status = "pending",
                PaymentId = paymentId,
                RawRequest = JsonSerializer.Serialize(requestBody),
                RawResponse = finikResponse.GetRawText(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentProviderTransactions.Add(providerTransaction);
            
            // Обновляем заказ
            order.PaymentMethod = "finik";
            order.PaymentStatus = "processing";
            order.Status = OrderStatus.Pending;

            await _context.SaveChangesAsync();

            return new FinikPaymentResponseDto
            {
                PaymentId = paymentId,
                PaymentUrl = paymentUrl,
                OrderId = orderId,
                Amount = amount,
                Status = "pending",
                Message = "Payment created successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Finik payment for order {OrderId}", orderId);
            throw;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<FinikWebhookDto> GetPaymentStatusAsync(string paymentId)
    {
        if (!_config.Enabled)
        {
            throw new InvalidOperationException("Finik payment processing is disabled");
        }

        try
        {
            // Добавляем авторизацию
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

            var response = await _httpClient.GetAsync($"/api/v1/payments/{paymentId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Finik API error getting payment status: {StatusCode}, {Content}", 
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Finik API error: {response.StatusCode}");
            }

            var paymentData = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            return new FinikWebhookDto
            {
                PaymentId = paymentData.GetProperty("id").GetString() ?? paymentId,
                OrderId = paymentData.TryGetProperty("external_id", out var externalId) 
                    ? int.TryParse(externalId.GetString(), out var orderId) ? orderId : null 
                    : null,
                Status = paymentData.GetProperty("status").GetString() ?? "unknown",
                Amount = paymentData.TryGetProperty("amount", out var amount) 
                    ? amount.GetDecimal() 
                    : null,
                Currency = paymentData.TryGetProperty("currency", out var currency) 
                    ? currency.GetString() 
                    : null,
                CreatedAt = paymentData.TryGetProperty("created_at", out var createdAt) 
                    ? DateTime.TryParse(createdAt.GetString(), out var created) ? created : null 
                    : null,
                UpdatedAt = paymentData.TryGetProperty("updated_at", out var updatedAt) 
                    ? DateTime.TryParse(updatedAt.GetString(), out var updated) ? updated : null 
                    : null,
                PaidAt = paymentData.TryGetProperty("paid_at", out var paidAt) 
                    ? DateTime.TryParse(paidAt.GetString(), out var paid) ? paid : null 
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Finik payment status for {PaymentId}", paymentId);
            throw;
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public bool VerifyWebhookSignature(string payload, string signature)
    {
        if (!_config.VerifySignature)
        {
            _logger.LogWarning("Signature verification is disabled");
            return true;
        }

        try
        {
            // Finik использует HMAC-SHA256 для подписи
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.ClientSecret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToBase64String(computedHash);

            var isValid = computedSignature == signature;
            
            if (!isValid)
            {
                _logger.LogWarning("Invalid webhook signature. Expected: {Expected}, Received: {Received}", 
                    computedSignature, signature);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    public async Task<bool> ProcessWebhookAsync(FinikWebhookDto webhook)
    {
        try
        {
            if (webhook.OrderId == null)
            {
                _logger.LogWarning("Webhook has no order_id, skipping");
                return false;
            }

            var orderId = webhook.OrderId.Value;
            _logger.LogInformation("Processing Finik webhook: PaymentId={PaymentId}, OrderId={OrderId}, Status={Status}", 
                webhook.PaymentId, orderId, webhook.Status);

            // Находим заказ
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for webhook", orderId);
                return false;
            }

            // Находим или создаем транзакцию провайдера
            var providerTransaction = await _context.PaymentProviderTransactions
                .FirstOrDefaultAsync(t => t.Qid == webhook.PaymentId && t.Provider == "finik");

            if (providerTransaction == null)
            {
                _logger.LogWarning("PaymentProviderTransaction not found for PaymentId={PaymentId}", webhook.PaymentId);
                // Создаем новую запись
                providerTransaction = new PaymentProviderTransaction
                {
                    Qid = webhook.PaymentId,
                    Provider = "finik",
                    OperationType = "webhook",
                    Account = order.UserId.ToString(),
                    Amount = webhook.Amount,
                    Status = webhook.Status,
                    PaymentId = webhook.PaymentId,
                    RawResponse = JsonSerializer.Serialize(webhook),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.PaymentProviderTransactions.Add(providerTransaction);
            }
            else
            {
                providerTransaction.Status = webhook.Status;
                providerTransaction.PaymentStatus = webhook.Status;
                providerTransaction.RawResponse = JsonSerializer.Serialize(webhook);
                providerTransaction.UpdatedAt = DateTime.UtcNow;
                providerTransaction.ProcessedAt = DateTime.UtcNow;
                providerTransaction.IsProcessed = true;
            }

            // Обновляем статус заказа в зависимости от статуса платежа
            bool orderUpdated = false;
            if (webhook.Status.Equals("success", StringComparison.OrdinalIgnoreCase) ||
                webhook.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
                webhook.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
            {
                if (order.Status == OrderStatus.Pending && order.PaymentStatus != "paid")
                {
                    order.Status = OrderStatus.Paid;
                    order.PaymentStatus = "paid";
                    order.PaidAt = webhook.PaidAt ?? DateTime.UtcNow;
                    orderUpdated = true;

                    // Создаем внутреннюю транзакцию
                    var transaction = new Transaction
                    {
                        UserId = order.UserId,
                        OrderId = order.Id,
                        Amount = webhook.Amount ?? order.FinalAmount,
                        Type = "payment",
                        Status = "completed",
                        Description = $"Оплата заказа #{order.Id} через Finik",
                        CreatedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    };
                    _context.Transactions.Add(transaction);
                    providerTransaction.InternalTransactionId = transaction.Id;
                }
            }
            else if (webhook.Status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                     webhook.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
            {
                order.PaymentStatus = webhook.Status.ToLower();
                if (webhook.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    order.Status = OrderStatus.Cancelled;
                    order.CancelledAt = DateTime.UtcNow;
                }
                orderUpdated = true;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Webhook processed successfully: OrderId={OrderId}, Status={Status}, OrderUpdated={OrderUpdated}", 
                orderId, webhook.Status, orderUpdated);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Finik webhook: PaymentId={PaymentId}", webhook.PaymentId);
            return false;
        }
    }
}

