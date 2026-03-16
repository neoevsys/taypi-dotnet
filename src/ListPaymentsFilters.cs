namespace Taypi
{
    /// <summary>
    /// Filtros para listar pagos.
    /// </summary>
    public class ListPaymentsFilters
    {
        /// <summary>
        /// Filtrar por estado: pending, completed, expired, cancelled, failed.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filtrar por referencia del comercio.
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Fecha inicio (YYYY-MM-DD).
        /// </summary>
        public string? From { get; set; }

        /// <summary>
        /// Fecha fin (YYYY-MM-DD).
        /// </summary>
        public string? To { get; set; }

        /// <summary>
        /// Resultados por pagina (default: 15).
        /// </summary>
        public int? PerPage { get; set; }

        /// <summary>
        /// Numero de pagina.
        /// </summary>
        public int? Page { get; set; }
    }
}
