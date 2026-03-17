using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Taypi;

namespace TaypiExamples
{
    /// <summary>
    /// Ejemplo: integración en Windows Forms (WinForms).
    ///
    /// ╔══════════════════════════════════════════════════════════════════╗
    /// ║  IMPORTANTE — ERRORES COMUNES QUE NUNCA DEBES COMETER         ║
    /// ║                                                                ║
    /// ║  ✗ NO uses RestSharp/HttpClient directo para llamar al API.    ║
    /// ║    Usa este SDK que firma los requests automáticamente.        ║
    /// ║                                                                ║
    /// ║  ✗ NUNCA envíes el secret key en headers ni en el body.        ║
    /// ║    El secret SOLO se usa localmente para calcular la firma     ║
    /// ║    HMAC-SHA256. NUNCA viaja por la red.                        ║
    /// ║                                                                ║
    /// ║  ✗ NUNCA hagas esto:                                          ║
    /// ║    request.AddHeader("Taypi-Signature", "TU_SECRET_KEY");      ║
    /// ║    request.AddHeader("Taypi-Timestamp", "TU_SECRET_KEY");      ║
    /// ║    Eso FILTRA tu clave secreta en cada request.                ║
    /// ║                                                                ║
    /// ║  ✓ Solo necesitas public key + secret key en el constructor.   ║
    /// ║    El SDK calcula la firma HMAC internamente.                  ║
    /// ╚══════════════════════════════════════════════════════════════════╝
    /// </summary>
    class WinFormsExample
    {
        // Inicializar UNA vez (reusar en toda la app)
        private static readonly TaypiClient taypi = new TaypiClient(
            publicKey: "taypi_pk_test_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
            secretKey: "taypi_sk_test_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
            // No hace falta BaseUrl: con keys _test_ auto-detecta sandbox
        );

        /// <summary>
        /// Ejemplo de botón "Cobrar" en un formulario de ventas.
        /// </summary>
        async Task btnCobrar_Click()
        {
            try
            {
                // Así de simple — el SDK firma todo automáticamente
                var session = await taypi.CreateCheckoutSessionAsync(
                    new PaymentParams
                    {
                        Amount = "50.00",
                        Reference = "VENTA-001",
                        Description = "Venta en tienda"
                    },
                    idempotencyKey: "VENTA-001"
                );

                MessageBox.Show($"Sesión creada: {session.CheckoutToken}");
            }
            catch (TaypiException ex) when (ex.ErrorCode == "KEY_URL_MISMATCH")
            {
                // Keys de test apuntando a producción o viceversa
                MessageBox.Show(
                    ex.Message,
                    "Error de configuración",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (TaypiException ex) when (ex.ErrorCode == "INVALID_KEY_FORMAT")
            {
                // Key mal formada (truncada, typo, etc.)
                MessageBox.Show(
                    ex.Message,
                    "API Key inválida",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (TaypiException ex) when (ex.HttpCode == 401)
            {
                // Key válida en formato pero revocada o incorrecta
                MessageBox.Show(
                    "Las API keys son inválidas o fueron revocadas. Genera nuevas keys en el panel de TAYPI.",
                    "Autenticación fallida",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (TaypiException ex) when (ex.HttpCode == 422)
            {
                // Error de validación (monto inválido, referencia duplicada, etc.)
                MessageBox.Show(
                    $"Error de validación: {ex.Message}",
                    "Datos inválidos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (TaypiException ex) when (ex.HttpCode == 429)
            {
                // Rate limit excedido
                MessageBox.Show(
                    "Demasiadas solicitudes. Espera un momento antes de intentar de nuevo.",
                    "Límite excedido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (TaypiException ex) when (ex.ErrorCode == "TIMEOUT")
            {
                MessageBox.Show(
                    "No se pudo conectar con TAYPI. Verifica tu conexión a internet.",
                    "Timeout",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (TaypiException ex) when (ex.ErrorCode == "CONNECTION_ERROR")
            {
                MessageBox.Show(
                    "Error de conexión con TAYPI. Verifica tu conexión a internet.",
                    "Sin conexión",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (TaypiException ex)
            {
                // Cualquier otro error de TAYPI
                MessageBox.Show(
                    $"{ex.Message}\n\nCódigo: {ex.ErrorCode}\nHTTP: {ex.HttpCode}",
                    "Error TAYPI",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                // Error inesperado (no de TAYPI)
                MessageBox.Show(
                    $"Error inesperado: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Ejemplo con polling: crear pago y esperar a que el cliente pague.
        /// Ideal para POS, kioscos, o formularios de venta.
        /// </summary>
        async Task btnCobrarConEspera_Click()
        {
            try
            {
                // 1. Crear pago
                var payment = await taypi.CreatePaymentAsync(
                    new PaymentParams
                    {
                        Amount = "25.00",
                        Reference = "POS-001",
                        Description = "Café"
                    },
                    idempotencyKey: "POS-001"
                );

                // 2. Mostrar QR al cliente (abrir en navegador o mostrar en pantalla)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = payment.CheckoutUrl,
                    UseShellExecute = true
                });

                // 3. Esperar hasta que pague (polling cada 3s, max 15 min)
                var result = await taypi.WaitForPaymentAsync(payment.PaymentId);

                if (result.Status == "completed")
                {
                    MessageBox.Show($"Pago recibido! Pagador: {result.PayerName}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"El pago terminó con estado: {result.Status}", "Pago no completado");
                }
            }
            catch (TimeoutException)
            {
                MessageBox.Show("El cliente no completó el pago en 15 minutos.");
            }
            catch (TaypiException ex)
            {
                MessageBox.Show($"Error: {ex.Message} ({ex.ErrorCode})");
            }
        }
    }
}
