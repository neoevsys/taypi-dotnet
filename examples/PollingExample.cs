using System;
using System.Threading;
using System.Threading.Tasks;
using Taypi;

namespace TaypiExamples
{
    /// <summary>
    /// Ejemplo: crear un pago y esperar (polling) hasta que se complete o expire.
    /// Ideal para POS, kioscos, WinForms, o server-to-server sin webhooks.
    /// </summary>
    class PollingExample
    {
        static async Task Main()
        {
            using var taypi = new TaypiClient(
                "taypi_pk_test_TU_PUBLIC_KEY_AQUI",
                "taypi_sk_test_TU_SECRET_KEY_AQUI"
            );

            try
            {
                // 1. Crear pago
                PaymentResponse payment = await taypi.CreatePaymentAsync(
                    new PaymentParams
                    {
                        Amount = "15.00",
                        Reference = "KIOSK-001",
                        Description = "Cafe americano"
                    },
                    idempotencyKey: "KIOSK-001"
                );

                Console.WriteLine($"Pago creado: {payment.PaymentId}");
                Console.WriteLine($"QR: {payment.CheckoutUrl}");
                Console.WriteLine("Esperando pago...");

                // 2. Esperar hasta que el cliente pague (polling cada 3 segundos, max 15 minutos)
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));

                PaymentResponse result = await taypi.WaitForPaymentAsync(
                    payment.PaymentId,
                    pollingIntervalSeconds: 3,
                    timeoutSeconds: 900,
                    cancellationToken: cts.Token
                );

                switch (result.Status)
                {
                    case "completed":
                        Console.WriteLine($"Pago completado! Pagador: {result.PayerName} paid_at: {result.PaidAt}");
                        break;

                    case "expired":
                        Console.WriteLine("El QR expiro (15 minutos). Generar nuevo pago si es necesario.");
                        break;

                    case "cancelled":
                        Console.WriteLine("El pago fue cancelado.");
                        break;

                    case "failed":
                        Console.WriteLine("El pago fallo.");
                        break;
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Timeout: el pago no se completo en el tiempo esperado.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operacion cancelada.");
            }
            catch (TaypiException ex)
            {
                Console.WriteLine($"Error TAYPI: {ex.Message} ({ex.ErrorCode})");
            }
        }
    }
}
