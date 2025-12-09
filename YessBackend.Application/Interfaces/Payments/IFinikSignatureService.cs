namespace YessBackend.Application.Interfaces.Payments;

/// <summary>
/// Интерфейс сервиса для генерации и проверки подписи Finik
/// </summary>
public interface IFinikSignatureService
{
    /// <summary>
    /// Генерирует каноническую строку для подписи
    /// </summary>
    string BuildCanonicalString(
        string httpMethod,
        string path,
        Dictionary<string, string> headers,
        Dictionary<string, string>? queryParameters = null,
        object? body = null);

    /// <summary>
    /// Генерирует RSA-SHA256 подпись и возвращает Base64 строку
    /// </summary>
    string GenerateSignature(string canonicalString, string privateKeyPem);

    /// <summary>
    /// Проверяет подпись используя публичный ключ Finik
    /// </summary>
    bool VerifySignature(
        string canonicalString,
        string signatureBase64,
        string publicKeyPem);
}

