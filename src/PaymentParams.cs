using System.Collections.Generic;

namespace Taypi
{
    /// <summary>
    /// Parametros para crear un pago o sesion de checkout.
    /// </summary>
    public class PaymentParams
    {
        /// <summary>
        /// Monto del pago en soles. Formato decimal con 2 decimales (ej: "25.00").
        /// Minimo S/1.00, maximo segun tier del comercio.
        /// </summary>
        public string Amount { get; set; } = "";

        /// <summary>
        /// Referencia unica del comercio (ej: numero de orden "ORD-12345").
        /// Maximo 100 caracteres.
        /// </summary>
        public string Reference { get; set; } = "";

        /// <summary>
        /// Descripcion del pago (opcional). Maximo 255 caracteres.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Metadata adicional (opcional). Pares clave-valor, valores string maximo 500 caracteres.
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
