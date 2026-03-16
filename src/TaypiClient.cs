using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Taypi
{
    /// <summary>
    /// Cliente oficial de TAYPI para pagos QR interoperables.
    /// Compatible con .NET Framework 4.8, .NET 7 y .NET 8.
    /// </summary>
    public class TaypiClient : IDisposable
    {
        private const string Version = "1.0.0";

        private static readonly string[] Environments =
        {
            "https://app.taypi.pe",
            "https://sandbox.taypi.pe"
        };

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Clave publica del comercio (taypi_pk_...).
        /// </summary>
        public string PublicKey { get; }

        private readonly string _secretKey;
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;

        /// <summary>
        /// Crea un nuevo cliente TAYPI.
        /// </summary>
        /// <param name="publicKey">Clave publica (taypi_pk_live_... o taypi_pk_test_...)</param>
        /// <param name="secretKey">Clave secreta (taypi_sk_live_... o taypi_sk_test_...) — nunca se envia en requests</param>
        /// <param name="options">Opciones de configuracion (opcional)</param>
        public TaypiClient(string publicKey, string secretKey, TaypiOptions? options = null)
            : this(publicKey, secretKey, options, null)
        {
        }

        /// <summary>
        /// Crea un nuevo cliente TAYPI con un HttpClient personalizado (para inyeccion de dependencias).
        /// </summary>
        /// <param name="publicKey">Clave publica</param>
        /// <param name="secretKey">Clave secreta</param>
        /// <param name="options">Opciones de configuracion (opcional)</param>
        /// <param name="httpClient">HttpClient personalizado (opcional). Si se provee, el caller es responsable de su lifecycle.</param>
        public TaypiClient(string publicKey, string secretKey, TaypiOptions? options, HttpClient? httpClient)
        {
            PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
            _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));

            var opts = options ?? new TaypiOptions();

            if (!string.IsNullOrEmpty(opts.BaseUrl))
            {
                var url = opts.BaseUrl!.TrimEnd('/');
                if (url.EndsWith("/v1"))
                    url = url.Substring(0, url.Length - 3);

                if (Array.IndexOf(Environments, url) < 0)
                {
                    throw new TaypiException(
                        "URL no permitida. Usa: app.taypi.pe o sandbox.taypi.pe",
                        "INVALID_BASE_URL");
                }

                _baseUrl = url;
            }
            else
            {
                _baseUrl = Environments[0];
            }

            if (httpClient != null)
            {
                _httpClient = httpClient;
                _ownsHttpClient = false;
            }
            else
            {
#if NET48 || NETSTANDARD2_0
                // .NET Framework: configurar TLS 1.2+
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
                _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(opts.Timeout) };
                _ownsHttpClient = true;
            }
        }

        // ─── Checkout Sessions ───────────────────────────────────

        /// <summary>
        /// Crea una sesion de checkout para usar con checkout.js.
        /// Retorna un diccionario con "checkout_token".
        /// </summary>
        /// <param name="parameters">Parametros del pago (amount, reference, description, metadata)</param>
        /// <param name="idempotencyKey">Clave unica para evitar pagos duplicados (ej: ID de orden)</param>
        /// <param name="cancellationToken">Token de cancelacion (opcional)</param>
        /// <returns>Diccionario con checkout_token</returns>
        public async Task<Dictionary<string, object?>> CreateCheckoutSessionAsync(
            PaymentParams parameters,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            var response = await PostAsync("/v1/checkout/sessions", ParamsToDict(parameters), idempotencyKey, cancellationToken).ConfigureAwait(false);
            return ExtractData(response);
        }

        // ─── Payments ────────────────────────────────────────────

        /// <summary>
        /// Crea un pago con QR. Retorna datos completos: payment_id, qr_code, checkout_url, etc.
        /// </summary>
        /// <param name="parameters">Parametros del pago</param>
        /// <param name="idempotencyKey">Clave unica para evitar pagos duplicados</param>
        /// <param name="cancellationToken">Token de cancelacion (opcional)</param>
        public async Task<Dictionary<string, object?>> CreatePaymentAsync(
            PaymentParams parameters,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            var response = await PostAsync("/api/v1/payments", ParamsToDict(parameters), idempotencyKey, cancellationToken).ConfigureAwait(false);
            return ExtractData(response);
        }

        /// <summary>
        /// Consulta un pago por ID.
        /// </summary>
        /// <param name="paymentId">UUID del pago</param>
        /// <param name="cancellationToken">Token de cancelacion (opcional)</param>
        public async Task<Dictionary<string, object?>> GetPaymentAsync(
            string paymentId,
            CancellationToken cancellationToken = default)
        {
            var response = await GetAsync($"/api/v1/payments/{paymentId}", cancellationToken).ConfigureAwait(false);
            return ExtractData(response);
        }

        /// <summary>
        /// Lista pagos del comercio con filtros opcionales.
        /// </summary>
        /// <param name="filters">Filtros opcionales (status, reference, from, to, per_page, page)</param>
        /// <param name="cancellationToken">Token de cancelacion (opcional)</param>
        public async Task<Dictionary<string, object?>> ListPaymentsAsync(
            ListPaymentsFilters? filters = null,
            CancellationToken cancellationToken = default)
        {
            var query = BuildQueryString(filters);
            var path = string.IsNullOrEmpty(query) ? "/api/v1/payments" : $"/api/v1/payments?{query}";
            return await GetAsync(path, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cancela un pago pendiente.
        /// </summary>
        /// <param name="paymentId">UUID del pago</param>
        /// <param name="idempotencyKey">Clave unica para evitar cancelaciones duplicadas</param>
        /// <param name="cancellationToken">Token de cancelacion (opcional)</param>
        public async Task<Dictionary<string, object?>> CancelPaymentAsync(
            string paymentId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            var response = await PostAsync($"/api/v1/payments/{paymentId}/cancel", new Dictionary<string, object?>(), idempotencyKey, cancellationToken).ConfigureAwait(false);
            return ExtractData(response);
        }

        /// <summary>
        /// Espera hasta que un pago alcance un estado terminal (completed, expired, cancelled, failed).
        /// Hace polling periodico al API. Ideal para integraciones server-to-server sin webhooks.
        /// </summary>
        /// <param name="paymentId">UUID del pago a monitorear</param>
        /// <param name="pollingIntervalSeconds">Intervalo entre consultas en segundos (default: 3, min: 1)</param>
        /// <param name="timeoutSeconds">Tiempo maximo de espera en segundos (default: 900 = 15 min)</param>
        /// <param name="cancellationToken">Token de cancelacion</param>
        /// <returns>Datos del pago con estado terminal</returns>
        /// <exception cref="TimeoutException">Si se supera el tiempo maximo de espera</exception>
        /// <exception cref="TaypiException">Si hay error de API o conexion</exception>
        public async Task<Dictionary<string, object?>> WaitForPaymentAsync(
            string paymentId,
            int pollingIntervalSeconds = 3,
            int timeoutSeconds = 900,
            CancellationToken cancellationToken = default)
        {
            if (pollingIntervalSeconds < 1)
                pollingIntervalSeconds = 1;

            var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);

            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var payment = await GetPaymentAsync(paymentId, cancellationToken).ConfigureAwait(false);
                var status = TryGetString(payment, "status") ?? "";

                if (status == "completed" || status == "expired" || status == "cancelled" || status == "failed")
                {
                    return payment;
                }

                await Task.Delay(TimeSpan.FromSeconds(pollingIntervalSeconds), cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException(
                $"El pago {paymentId} no alcanzo un estado terminal en {timeoutSeconds} segundos.");
        }

        // ─── Webhooks ────────────────────────────────────────────

        /// <summary>
        /// Verifica la firma HMAC-SHA256 de un webhook recibido.
        /// Usar antes de procesar cualquier evento de webhook.
        /// </summary>
        /// <param name="payload">Body crudo del webhook (raw string)</param>
        /// <param name="signature">Valor del header Taypi-Signature (sha256=...)</param>
        /// <param name="webhookSecret">Secret del webhook del comercio</param>
        /// <returns>true si la firma es valida</returns>
        public static bool VerifyWebhook(string payload, string signature, string webhookSecret)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(webhookSecret))
                return false;

            var expected = "sha256=" + ComputeHmacSha256(webhookSecret, payload);

            // Comparacion timing-safe
            return FixedTimeEquals(expected, signature);
        }

        // ─── HTTP ────────────────────────────────────────────────

        private Task<Dictionary<string, object?>> PostAsync(
            string path,
            Dictionary<string, object?> parameters,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            return RequestAsync("POST", path, parameters, idempotencyKey, cancellationToken);
        }

        private Task<Dictionary<string, object?>> GetAsync(
            string path,
            CancellationToken cancellationToken)
        {
            return RequestAsync("GET", path, null, null, cancellationToken);
        }

        private async Task<Dictionary<string, object?>> RequestAsync(
            string method,
            string path,
            Dictionary<string, object?>? parameters,
            string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            var url = _baseUrl + path;
            var body = parameters != null ? JsonSerializer.Serialize(parameters, JsonOptions) : "";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

            // Extraer solo el path de la URL para la firma
            var uri = new Uri(url);
            var signaturePath = uri.AbsolutePath;

            // Firma HMAC-SHA256: timestamp\nmethod\npath\nbody
            var message = $"{timestamp}\n{method}\n{signaturePath}\n{body}";
            var hmacSignature = ComputeHmacSha256(_secretKey, message);

            var request = new HttpRequestMessage(new HttpMethod(method), url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", PublicKey);
            request.Headers.TryAddWithoutValidation("Taypi-Signature", hmacSignature);
            request.Headers.TryAddWithoutValidation("Taypi-Timestamp", timestamp);
            request.Headers.TryAddWithoutValidation("User-Agent", $"taypi-dotnet/{Version}");

            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            }

            if (method == "POST" && body.Length > 0)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage httpResponse;
            try
            {
                httpResponse = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TaypiException("Timeout de conexion", "TIMEOUT");
            }
            catch (HttpRequestException ex)
            {
                throw new TaypiException(
                    $"Error de conexion: {ex.Message}",
                    "CONNECTION_ERROR",
                    innerException: ex);
            }

            var responseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var httpCode = (int)httpResponse.StatusCode;

            Dictionary<string, object?>? data;
            try
            {
                data = JsonSerializer.Deserialize<Dictionary<string, object?>>(responseBody, JsonOptions);
            }
            catch
            {
                throw new TaypiException(
                    "Respuesta invalida del servidor",
                    "INVALID_RESPONSE",
                    httpCode);
            }

            if (data == null)
            {
                throw new TaypiException(
                    "Respuesta invalida del servidor",
                    "INVALID_RESPONSE",
                    httpCode);
            }

            if (httpCode >= 400)
            {
                var errorMessage = TryGetString(data, "message") ?? "Error del API";
                var errorCode = TryGetString(data, "error") ?? "API_ERROR";

                throw new TaypiException(errorMessage, errorCode, httpCode, data);
            }

            return data;
        }

        // ─── Helpers ─────────────────────────────────────────────

        private static Dictionary<string, object?> ParamsToDict(PaymentParams p)
        {
            var dict = new Dictionary<string, object?>
            {
                ["amount"] = p.Amount,
                ["reference"] = p.Reference
            };

            if (p.Description != null)
                dict["description"] = p.Description;

            if (p.Metadata != null)
                dict["metadata"] = p.Metadata;

            return dict;
        }

        private static Dictionary<string, object?> ExtractData(Dictionary<string, object?> response)
        {
            if (response.TryGetValue("data", out var dataObj) && dataObj is JsonElement element)
            {
                var dataDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText(), JsonOptions);
                return dataDict ?? response;
            }

            return response;
        }

        private static string? TryGetString(Dictionary<string, object?> dict, string key)
        {
            if (!dict.TryGetValue(key, out var value) || value == null)
                return null;

            if (value is JsonElement el)
                return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();

            return value.ToString();
        }

        private static string ComputeHmacSha256(string key, string data)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Comparacion de strings en tiempo constante para prevenir timing attacks.
        /// </summary>
        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length)
                return false;

            var result = 0;
            for (var i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }

        private static string BuildQueryString(ListPaymentsFilters? filters)
        {
            if (filters == null) return "";

            var parts = new List<string>();

            if (!string.IsNullOrEmpty(filters.Status))
                parts.Add($"status={Uri.EscapeDataString(filters.Status!)}");
            if (!string.IsNullOrEmpty(filters.Reference))
                parts.Add($"reference={Uri.EscapeDataString(filters.Reference!)}");
            if (!string.IsNullOrEmpty(filters.From))
                parts.Add($"from={Uri.EscapeDataString(filters.From!)}");
            if (!string.IsNullOrEmpty(filters.To))
                parts.Add($"to={Uri.EscapeDataString(filters.To!)}");
            if (filters.PerPage.HasValue)
                parts.Add($"per_page={filters.PerPage.Value}");
            if (filters.Page.HasValue)
                parts.Add($"page={filters.Page.Value}");

            return string.Join("&", parts);
        }

        /// <summary>
        /// Libera los recursos del HttpClient si fue creado internamente.
        /// </summary>
        public void Dispose()
        {
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
        }
    }
}
