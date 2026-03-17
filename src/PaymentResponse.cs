using System.Text.Json.Serialization;

namespace Taypi
{
    /// <summary>
    /// Respuesta al crear o consultar un pago.
    /// </summary>
    public class PaymentResponse
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

        /// <summary>
        /// Texto del QR — el contenido que se codifica dentro del codigo QR.
        /// NO es una imagen. Es el string que se puede pasar a cualquier
        /// libreria generadora de QR (ZXing, QRCoder, etc.) para crear tu propia imagen.
        /// Ejemplo: "0002010102122637000280010390302..."
        /// </summary>
        [JsonPropertyName("qr_code")]
        public string? QrText { get; set; }

        /// <summary>
        /// Imagen QR lista para mostrar, en formato data URI (SVG base64).
        /// Ejemplo: "data:image/svg+xml;base64,PD94bWwg..."
        ///
        /// Para usar en WinForms/WPF:
        ///   - Extraer el base64 despues de la coma
        ///   - Decodificar a bytes
        ///   - Cargar como imagen
        ///
        /// Para usar en HTML:
        ///   - Usar directamente como src de un img tag
        /// </summary>
        [JsonPropertyName("qr_image")]
        public string? QrImage { get; set; }

        /// <summary>
        /// URL de la pagina de checkout (para redirigir al cliente o abrir en navegador).
        /// Ejemplo: "https://sandbox.taypi.pe/qr/cRP2tzLzBW"
        /// </summary>
        [JsonPropertyName("checkout_url")]
        public string CheckoutUrl { get; set; } = "";

        /// <summary>
        /// Hash corto para URL corta (/qr/{hash}).
        /// </summary>
        [JsonPropertyName("short_hash")]
        public string? ShortHash { get; set; }

        /// <summary>
        /// Token de checkout (para usar con checkout.js o GetCheckoutSessionAsync).
        /// </summary>
        [JsonPropertyName("checkout_token")]
        public string? CheckoutToken { get; set; }

        [JsonPropertyName("expires_at")]
        public string ExpiresAt { get; set; } = "";

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = "";

        /// <summary>
        /// Nombre del pagador (solo disponible despues de completarse).
        /// </summary>
        [JsonPropertyName("payer_name")]
        public string? PayerName { get; set; }

        /// <summary>
        /// Billetera del pagador: yape, plin, etc. (solo despues de completarse).
        /// </summary>
        [JsonPropertyName("payer_wallet")]
        public string? PayerWallet { get; set; }

        [JsonPropertyName("paid_at")]
        public string? PaidAt { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("merchant_id")]
        public string? MerchantId { get; set; }
    }
}
