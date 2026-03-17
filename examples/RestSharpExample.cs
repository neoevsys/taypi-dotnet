using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RestSharp;

namespace TaypiExamples
{
    /// <summary>
    /// Ejemplo: integración con RestSharp SIN el SDK de TAYPI.
    ///
    /// Si prefieres usar RestSharp directamente en vez del SDK,
    /// este ejemplo muestra cómo firmar correctamente los requests.
    ///
    /// ╔══════════════════════════════════════════════════════════════════╗
    /// ║  REGLA DE ORO: el secret key NUNCA viaja en los requests.      ║
    /// ║  Solo se usa LOCALMENTE para calcular la firma HMAC-SHA256.    ║
    /// ║                                                                ║
    /// ║  ✗ MAL:  AddHeader("Taypi-Signature", "taypi_sk_test_...");    ║
    /// ║  ✓ BIEN: AddHeader("Taypi-Signature", ComputeHmac(...));      ║
    /// ╚══════════════════════════════════════════════════════════════════╝
    /// </summary>
    class RestSharpExample
    {
        // ── Tus keys (NUNCA hardcodear en código — usar app.config, .env, etc.) ──
        private const string PublicKey = "taypi_pk_test_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
        private const string SecretKey = "taypi_sk_test_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

        // Sandbox: https://sandbox.taypi.pe — Producción: https://app.taypi.pe
        private const string BaseUrl = "https://sandbox.taypi.pe";

        static void Main()
        {
            // ─── Crear sesión de checkout ──────────────────────────
            var body = new Dictionary<string, object>
            {
                ["amount"] = "50.00",
                ["reference"] = "VENTA-001",
                ["description"] = "Venta en tienda"
            };

            var jsonBody = JsonSerializer.Serialize(body);
            var method = "POST";
            var path = "/v1/checkout/sessions";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            // ── Calcular firma HMAC-SHA256 ──
            // Formato: timestamp\nMETHOD\n/path\n{body}
            var message = $"{timestamp}\n{method}\n{path}\n{jsonBody}";
            var signature = ComputeHmacSha256(SecretKey, message);

            // ── Armar request ──
            var client = new RestClient(BaseUrl);
            var request = new RestRequest(path, Method.Post);

            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {PublicKey}");       // ← PUBLIC key, no secret
            request.AddHeader("Taypi-Signature", signature);                 // ← HMAC calculado, no la key
            request.AddHeader("Taypi-Timestamp", timestamp);                 // ← Unix timestamp en segundos
            request.AddHeader("Idempotency-Key", "VENTA-001");              // ← ID único de la operación
            request.AddHeader("User-Agent", "mi-app/1.0");

            request.AddStringBody(jsonBody, ContentType.Json);

            var response = client.Execute(request);

            if (response.IsSuccessful)
            {
                Console.WriteLine($"Respuesta: {response.Content}");
            }
            else
            {
                Console.WriteLine($"Error HTTP {(int)response.StatusCode}: {response.Content}");
            }

            // ─── Consultar un pago (GET) ──────────────────────────
            var paymentId = "uuid-del-pago";
            var getPath = $"/api/v1/payments/{paymentId}";
            var getTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            // Para GET el body es vacío
            var getMessage = $"{getTimestamp}\nGET\n{getPath}\n";
            var getSignature = ComputeHmacSha256(SecretKey, getMessage);

            var getRequest = new RestRequest(getPath, Method.Get);
            getRequest.AddHeader("Accept", "application/json");
            getRequest.AddHeader("Authorization", $"Bearer {PublicKey}");
            getRequest.AddHeader("Taypi-Signature", getSignature);
            getRequest.AddHeader("Taypi-Timestamp", getTimestamp);

            var getResponse = client.Execute(getRequest);
            Console.WriteLine($"Pago: {getResponse.Content}");
        }

        /// <summary>
        /// Calcula HMAC-SHA256 y retorna el digest en hexadecimal lowercase.
        /// Esta es la ÚNICA forma correcta de generar Taypi-Signature.
        /// </summary>
        private static string ComputeHmacSha256(string key, string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
