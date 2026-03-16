using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Taypi;

namespace TaypiExamples
{
    /// <summary>
    /// Ejemplo: operaciones CRUD de pagos via API directa.
    /// </summary>
    class PaymentsExample
    {
        static async Task Main()
        {
            using var taypi = new TaypiClient(
                "taypi_pk_test_TU_PUBLIC_KEY_AQUI",
                "taypi_sk_test_TU_SECRET_KEY_AQUI",
                new TaypiOptions { BaseUrl = "https://sandbox.taypi.pe" }
            );

            try
            {
                // ─── Crear pago ──────────────────────────────────
                var payment = await taypi.CreatePaymentAsync(
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

                Console.WriteLine($"Pago creado: {payment["payment_id"]}");
                Console.WriteLine($"QR checkout: {payment["checkout_url"]}");

                // ─── Consultar pago ──────────────────────────────
                var paymentId = payment["payment_id"]?.ToString() ?? "";
                var detail = await taypi.GetPaymentAsync(paymentId);
                Console.WriteLine($"Estado: {detail["status"]}");

                // ─── Listar pagos ────────────────────────────────
                var list = await taypi.ListPaymentsAsync(new ListPaymentsFilters
                {
                    Status = "completed",
                    From = "2026-03-01",
                    To = "2026-03-31",
                    PerPage = 20
                });
                Console.WriteLine($"Pagos: {list["data"]}");

                // ─── Cancelar pago pendiente ─────────────────────
                var cancelled = await taypi.CancelPaymentAsync(paymentId, "cancel-ORD-789");
                Console.WriteLine($"Cancelado: {cancelled["status"]}");
            }
            catch (TaypiException ex)
            {
                Console.WriteLine($"Error TAYPI: {ex.Message} ({ex.ErrorCode}) HTTP {ex.HttpCode}");
            }
        }
    }
}
