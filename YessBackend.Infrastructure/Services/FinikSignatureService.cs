using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YessBackend.Application.Interfaces.Payments;

namespace YessBackend.Infrastructure.Services;

/// <summary>
/// Сервис для генерации и проверки подписи Finik согласно спецификации
/// </summary>
public class FinikSignatureService : IFinikSignatureService
{
    private readonly ILogger<FinikSignatureService> _logger;

    public FinikSignatureService(ILogger<FinikSignatureService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Генерирует каноническую строку для подписи согласно спецификации Finik
    /// </summary>
    public string BuildCanonicalString(
        string httpMethod,
        string path,
        Dictionary<string, string> headers,
        Dictionary<string, string>? queryParameters = null,
        object? body = null)
    {
        var canonical = new StringBuilder();

        // 1. Lowercase HTTP method
        canonical.AppendLine(httpMethod.ToLowerInvariant());

        // 2. URI absolute path (without query)
        canonical.AppendLine(path);

        // 3. Headers: Host + all x-api-* headers, sorted alphabetically
        var headerParts = new List<string>();
        
        // Add Host header
        if (headers.TryGetValue("Host", out var hostValue))
        {
            headerParts.Add($"host:{hostValue}");
        }

        // Add all x-api-* headers
        foreach (var header in headers)
        {
            if (header.Key.StartsWith("x-api-", StringComparison.OrdinalIgnoreCase))
            {
                var key = header.Key.ToLowerInvariant();
                headerParts.Add($"{key}:{header.Value}");
            }
        }

        // Sort alphabetically
        headerParts.Sort();

        // Concatenate with &
        if (headerParts.Count > 0)
        {
            canonical.AppendLine(string.Join("&", headerParts));
        }
        else
        {
            canonical.AppendLine();
        }

        // 4. Query string parameters (sorted, URI encoded)
        if (queryParameters != null && queryParameters.Count > 0)
        {
            var queryParts = new List<string>();
            foreach (var param in queryParameters.OrderBy(p => p.Key))
            {
                var encodedKey = Uri.EscapeDataString(param.Key);
                var encodedValue = string.IsNullOrEmpty(param.Value) 
                    ? string.Empty 
                    : Uri.EscapeDataString(param.Value);
                queryParts.Add($"{encodedKey}={encodedValue}");
            }
            canonical.AppendLine(string.Join("&", queryParts));
        }
        else
        {
            // Note: Don't add "\n" if there are no query parameters
            // But we already added a newline after headers, so we skip adding another
        }

        // 5. JSON body (sorted by keys, compact JSON, no spaces)
        if (body != null)
        {
            // Сериализуем с правильными именами свойств (PascalCase для верхнего уровня)
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = null // PascalCase по умолчанию для верхнего уровня
            };

            var jsonString = JsonSerializer.Serialize(body, jsonOptions);
            var jsonDoc = JsonDocument.Parse(jsonString);
            
            // Сортируем JSON по ключам рекурсивно
            var sortedJson = SortJsonObject(jsonDoc.RootElement);
            
            // Сериализуем отсортированный JSON без пробелов (compact JSON)
            var sortedJsonString = JsonSerializer.Serialize(sortedJson, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            
            canonical.Append(sortedJsonString);
        }

        var canonicalString = canonical.ToString();
        _logger.LogDebug("Canonical string for signature: {Canonical}", canonicalString);
        
        return canonicalString;
    }

    /// <summary>
    /// Генерирует RSA-SHA256 подпись и возвращает Base64 строку
    /// </summary>
    public string GenerateSignature(string canonicalString, string privateKeyPem)
    {
        try
        {
            // Parse private key from PEM
            var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            // Sign with SHA256
            var dataBytes = Encoding.UTF8.GetBytes(canonicalString);
            var signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            // Return Base64
            return Convert.ToBase64String(signatureBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка генерации подписи Finik");
            throw new InvalidOperationException("Не удалось сгенерировать подпись", ex);
        }
    }

    /// <summary>
    /// Проверяет подпись используя публичный ключ Finik
    /// </summary>
    public bool VerifySignature(
        string canonicalString,
        string signatureBase64,
        string publicKeyPem)
    {
        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            var dataBytes = Encoding.UTF8.GetBytes(canonicalString);
            var signatureBytes = Convert.FromBase64String(signatureBase64);

            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка проверки подписи Finik");
            return false;
        }
    }

    /// <summary>
    /// Сортирует JSON объект по ключам для канонизации
    /// </summary>
    private Dictionary<string, object?> SortJsonObject(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in element.EnumerateObject().OrderBy(p => p.Name))
            {
                dict[prop.Name] = SortJsonValue(prop.Value);
            }
            return dict;
        }
        
        return new Dictionary<string, object?> { { "value", SortJsonValue(element) } };
    }

    private object? SortJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => SortJsonObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(SortJsonValue).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}

