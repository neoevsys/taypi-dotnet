using System;
using System.Threading.Tasks;
using Taypi;

namespace TaypiExamples
{
    /// <summary>
    /// Ejemplo: crear sesion de checkout y mostrar el token para checkout.js
    /// </summary>
    class CheckoutExample
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
                var session = await taypi.CreateCheckoutSessionAsync(
                    new PaymentParams
                    {
                        Amount = "25.00",
                        Reference = "ORD-12345",
                        Description = "Zapatillas Nike Air"
                    },
                    idempotencyKey: "ORD-12345"
                );

                Console.WriteLine($"Checkout token: {session["checkout_token"]}");

                // Usar en frontend:
                // <script src="https://app.taypi.pe/v1/checkout.js"></script>
                // Taypi.publicKey = 'taypi_pk_test_...';
                // Taypi.open({ sessionToken: 'TOKEN_AQUI', onSuccess: ... });
            }
            catch (TaypiException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Codigo: {ex.ErrorCode}");
                Console.WriteLine($"HTTP: {ex.HttpCode}");
            }
        }
    }
}
