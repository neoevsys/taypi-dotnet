using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Taypi
{
    /// <summary>
    /// Respuesta al listar pagos (GET /api/v1/payments).
    /// </summary>
    public class PaymentListResponse
    {
        [JsonPropertyName("data")]
        public List<PaymentResponse> Data { get; set; } = new List<PaymentResponse>();

        [JsonPropertyName("meta")]
        public PaginationMeta Meta { get; set; } = new PaginationMeta();
    }

    public class PaginationMeta
    {
        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }
    }
}
