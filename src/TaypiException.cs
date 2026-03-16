using System;
using System.Collections.Generic;

namespace Taypi
{
    /// <summary>
    /// Excepcion lanzada cuando el API de TAYPI retorna un error o hay problemas de conexion.
    /// </summary>
    public class TaypiException : Exception
    {
        /// <summary>
        /// Codigo de error TAYPI (ej: AUTH_KEY_INVALID, PAYMENT_EXPIRED, RATE_LIMIT_EXCEEDED).
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// HTTP status code. 0 si es error de conexion o timeout.
        /// </summary>
        public int HttpCode { get; }

        /// <summary>
        /// Respuesta completa del API como diccionario, o null si no hubo respuesta parseable.
        /// </summary>
        public Dictionary<string, object?>? Response { get; }

        /// <summary>
        /// Crea una nueva instancia de TaypiException.
        /// </summary>
        public TaypiException(
            string message,
            string errorCode = "UNKNOWN",
            int httpCode = 0,
            Dictionary<string, object?>? response = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            HttpCode = httpCode;
            Response = response;
        }
    }
}
