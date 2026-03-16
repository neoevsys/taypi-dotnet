using System;
using Taypi;

namespace TaypiExamples
{
    /// <summary>
    /// Ejemplo: verificar firma de webhook en ASP.NET / ASP.NET Core.
    /// </summary>
    class WebhookExample
    {
        // ─── ASP.NET Core (Minimal API) ──────────────────────────
        //
        // app.MapPost("/webhooks/taypi", async (HttpContext context) =>
        // {
        //     using var reader = new StreamReader(context.Request.Body);
        //     var payload = await reader.ReadToEndAsync();
        //     var signature = context.Request.Headers["Taypi-Signature"].ToString();
        //     var webhookSecret = "tu_webhook_secret";
        //
        //     if (!TaypiClient.VerifyWebhook(payload, signature, webhookSecret))
        //     {
        //         return Results.StatusCode(403);
        //     }
        //
        //     var data = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);
        //     // Procesar evento...
        //     Console.WriteLine($"Evento: {data["event"]}");
        //
        //     return Results.Ok();
        // });

        // ─── ASP.NET Core (Controller) ───────────────────────────
        //
        // [HttpPost("webhooks/taypi")]
        // public async Task<IActionResult> HandleWebhook()
        // {
        //     using var reader = new StreamReader(Request.Body);
        //     var payload = await reader.ReadToEndAsync();
        //     var signature = Request.Headers["Taypi-Signature"].ToString();
        //
        //     if (!TaypiClient.VerifyWebhook(payload, signature, _webhookSecret))
        //         return StatusCode(403);
        //
        //     var data = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);
        //     // Procesar: payment.completed, payment.expired, etc.
        //     return Ok();
        // }

        // ─── ASP.NET Framework (Web API 2) ───────────────────────
        //
        // [HttpPost]
        // [Route("api/webhooks/taypi")]
        // public async Task<IHttpActionResult> HandleWebhook()
        // {
        //     var payload = await Request.Content.ReadAsStringAsync();
        //     IEnumerable<string> sigValues;
        //     Request.Headers.TryGetValues("Taypi-Signature", out sigValues);
        //     var signature = sigValues?.FirstOrDefault() ?? "";
        //
        //     if (!TaypiClient.VerifyWebhook(payload, signature, webhookSecret))
        //         return StatusCode(HttpStatusCode.Forbidden);
        //
        //     // Procesar evento...
        //     return Ok();
        // }

        static void Main()
        {
            // Demo de verificacion standalone
            var payload = "{\"event\":\"payment.completed\",\"payment_id\":\"abc-123\",\"amount\":\"50.00\"}";
            var secret = "test_webhook_secret";

            // Simular firma (en produccion viene del header Taypi-Signature)
            using var hmac = new System.Security.Cryptography.HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
            var signature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            var isValid = TaypiClient.VerifyWebhook(payload, signature, secret);
            Console.WriteLine($"Webhook valido: {isValid}"); // true
        }
    }
}
