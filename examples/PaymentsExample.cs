using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Taypi;

namespace TaypiExamples
{
    /// <summary>
    /// Ejemplo: operaciones CRUD de pagos via API directa.
    /// Recomendado para WinForms, POS, kioscos, server-to-server.
    /// </summary>
    class PaymentsExample
    {
        static async Task Main()
        {
            using var taypi = new TaypiClient(
                "taypi_pk_test_TU_PUBLIC_KEY_AQUI",
                "taypi_sk_test_TU_SECRET_KEY_AQUI"
            );

            try
            {
                // ─── Crear pago ──────────────────────────────────
                PaymentResponse payment = await taypi.CreatePaymentAsync(
                    new PaymentParams
                    {
                        Amount = "50.00",
                        Reference = "ORD-789",
                        Description = "Camiseta Peru",
                        Metadata = new Dictionary<string, string>
                        {
                            ["source"] = "web",
                            ["color"] = "blanco"
                        }
                    },
                    idempotencyKey: "ORD-789"
                );

                Console.WriteLine($"Pago creado: {payment.PaymentId}");
                Console.WriteLine($"QR checkout: {payment.CheckoutUrl}");
                Console.WriteLine($"QR texto: {payment.QrText}");
                Console.WriteLine($"QR imagen: {payment.QrImage?.Substring(0, 50)}...");
                Console.WriteLine($"Expira: {payment.ExpiresAt}");

                // ─── Consultar pago ──────────────────────────────
                PaymentResponse detail = await taypi.GetPaymentAsync(payment.PaymentId);
                Console.WriteLine($"Estado: {detail.Status}");

                // ─── Listar pagos ────────────────────────────────
                PaymentListResponse list = await taypi.ListPaymentsAsync(new ListPaymentsFilters
                {
                    Status = "completed",
                    From = "2026-03-01",
                    To = "2026-03-31",
                    PerPage = 20
                });
                Console.WriteLine($"Total pagos: {list.Meta.Total}");
                foreach (var p in list.Data)
                    Console.WriteLine($"  {p.PaymentId} — S/{p.Amount} — {p.Status}");

                // ─── Cancelar pago pendiente ─────────────────────
                PaymentResponse cancelled = await taypi.CancelPaymentAsync(payment.PaymentId, "cancel-ORD-789");
                Console.WriteLine($"Cancelado: {cancelled.Status}");
            }
            catch (TaypiException ex)
            {
                Console.WriteLine($"Error TAYPI: {ex.Message} ({ex.ErrorCode}) HTTP {ex.HttpCode}");
            }
        }
    }
}
