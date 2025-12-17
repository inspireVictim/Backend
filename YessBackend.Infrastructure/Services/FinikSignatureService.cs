using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using YessBackend.Application.Interfaces.Payments;

namespace YessBackend.Infrastructure.Services
{
    public class FinikSignatureService : IFinikSignatureService
    {
        public string BuildCanonicalString(
            string httpMethod,
            string path,
            Dictionary<string, string> headers,
            Dictionary<string, string>? queryParameters = null,
            object? body = null)
        {
            var sb = new StringBuilder();

            // 1️⃣ HTTP METHOD
            sb.Append(httpMethod.ToLowerInvariant()).Append('\n');

            // 2️⃣ PATH
            sb.Append(path).Append('\n');

            // 3️⃣ HEADERS (host + x-api-*)
            var canonicalHeaders = headers
                .Where(h =>
                    h.Key.Equals("host", StringComparison.OrdinalIgnoreCase) ||
                    h.Key.StartsWith("x-api-", StringComparison.OrdinalIgnoreCase))
                .OrderBy(h => h.Key, StringComparer.Ordinal)
                .Select(h => $"{h.Key.ToLowerInvariant()}:{h.Value}");

            sb.Append(string.Join("&", canonicalHeaders)).Append('\n');

            // 4️⃣ BODY
            if (body == null)
            {
                // nothing
            }
            else if (body is string rawJson)
            {
                // 🔥 КЛЮЧЕВО: используем готовый отсортированный JSON
                sb.Append(rawJson);
            }
            else
            {
                // fallback (если вдруг передали object)
                string json = JsonSerializer.Serialize(body);
                sb.Append(SortJson(json));
            }

            return sb.ToString();
        }

        // ===== JSON SORTING =====

        public string SortJson(string json)
        {
            var node = JsonNode.Parse(json);
            if (node == null)
                return json;

            var sorted = SortNode(node);
            return sorted.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        private JsonNode SortNode(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                var sortedObj = new JsonObject();

                foreach (var prop in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    sortedObj[prop.Key] = SortNode(prop.Value!);
                }

                return sortedObj;
            }

            if (node is JsonArray arr)
            {
                var newArr = new JsonArray();
                foreach (var item in arr)
                    newArr.Add(SortNode(item!));

                return newArr;
            }

            return node.DeepClone();
        }

        // ===== SIGNATURE =====

        public string GenerateSignature(string canonicalString, string privateKeyPem)
        {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            byte[] data = Encoding.UTF8.GetBytes(canonicalString);
            byte[] signature = rsa.SignData(
                data,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            return Convert.ToBase64String(signature);
        }

        public bool VerifySignature(
            string canonicalString,
            string signatureBase64,
            string publicKeyPem)
        {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            byte[] data = Encoding.UTF8.GetBytes(canonicalString);
            byte[] signature = Convert.FromBase64String(signatureBase64);

            return rsa.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
        }
    }
}
