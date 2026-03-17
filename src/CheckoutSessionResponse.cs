using System.Text.Json.Serialization;

namespace Taypi
{
    /// <summary>
    /// Respuesta al crear una sesion de checkout (POST /v1/checkout/sessions).
    /// Solo contiene el checkout_token. Para obtener datos del QR, usar GetCheckoutSessionAsync.
    /// </summary>
    public class CheckoutSessionResponse
    {
        /// <summary>
        /// Token para usar con checkout.js: Taypi.open({ sessionToken: "..." })
        /// O para consultar datos del QR via GetCheckoutSessionAsync.
        /// </summary>
        [JsonPropertyName("checkout_token")]
        public string CheckoutToken { get; set; } = "";
    }

    /// <summary>
    /// Datos completos de una sesion de checkout (GET /v1/checkout/sessions/{token}).
    /// Incluye imagen QR, monto, estado, etc.
    /// </summary>
    public class CheckoutSessionDetail
    {
        [JsonPropertyName("payment_id")]
        public string PaymentId { get; set; } = "";

        [JsonPropertyName("amount")]
        public string Amount { get; set; } = "";

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("reference")]
        public string Reference { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Imagen QR en base64 SVG. null si el pago ya no esta pendiente.
        /// </summary>
        [JsonPropertyName("qr_image")]
        public string? QrImage { get; set; }

        /// <summary>
        /// Nombre del comercio (business_name del merchant).
        /// </summary>
        [JsonPropertyName("merchant_name")]
        public string? MerchantName { get; set; }

        /// <summary>
        /// Nombre de la tienda/sucursal (si aplica).
        /// </summary>
        [JsonPropertyName("store_name")]
        public string? StoreName { get; set; }

        [JsonPropertyName("expires_at")]
        public string? ExpiresAt { get; set; }

        [JsonPropertyName("paid_at")]
        public string? PaidAt { get; set; }
    }
}
