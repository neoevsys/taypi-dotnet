namespace Taypi
{
    /// <summary>
    /// Opciones de configuracion para el cliente TAYPI.
    /// </summary>
    public class TaypiOptions
    {
        /// <summary>
        /// URL base del API. Por defecto: https://app.taypi.pe
        /// Valores permitidos: https://app.taypi.pe, https://sandbox.taypi.pe
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Timeout en segundos para requests HTTP. Por defecto: 15.
        /// </summary>
        public int Timeout { get; set; } = 15;
    }
}
