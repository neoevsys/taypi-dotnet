using System;
using System.Threading.Tasks;
using Taypi;

namespace TaypiExamples
{
    /// <summary>
    /// Ejemplo: crear sesion de checkout y consultar datos del QR.
    /// Flujo para integraciones web con checkout.js.
    /// </summary>
    class CheckoutExample
    {
        static async Task Main()
        {
            // Auto-detecta sandbox por el prefijo _test_ de las keys
            using var taypi = new TaypiClient(
                "taypi_pk_test_TU_PUBLIC_KEY_AQUI",
                "taypi_sk_test_TU_SECRET_KEY_AQUI"
            );

            try
            {
                // Paso 1: crear sesion (retorna solo checkout_token)
                CheckoutSessionResponse session = await taypi.CreateCheckoutSessionAsync(
                    new PaymentParams
                    {
                        Amount = "25.00",
                        Reference = "ORD-12345",
                        Description = "Zapatillas Nike Air"
                    },
                    idempotencyKey: "ORD-12345"
                );

                Console.WriteLine($"Checkout token: {session.CheckoutToken}");

                // Paso 2: consultar datos completos (QR, monto, merchant, etc.)
                CheckoutSessionDetail detail = await taypi.GetCheckoutSessionAsync(session.CheckoutToken);

                Console.WriteLine($"Payment ID: {detail.PaymentId}");
                Console.WriteLine($"Monto: {detail.Amount} {detail.Currency}");
                Console.WriteLine($"Comercio: {detail.MerchantName}");
                Console.WriteLine($"QR Image: {detail.QrImage?.Substring(0, 50)}...");
                Console.WriteLine($"Expira: {detail.ExpiresAt}");

                // En frontend usar checkout.js:
                // <script src="https://app.taypi.pe/v1/checkout.js"></script>
                // Taypi.publicKey = 'taypi_pk_test_...';
                // Taypi.open({ sessionToken: 'TOKEN_AQUI', onSuccess: ... });
            }
            catch (TaypiException ex)
            {
                Console.WriteLine($"Error: {ex.Message} ({ex.ErrorCode}) HTTP {ex.HttpCode}");
            }
        }
    }
}
