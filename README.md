# TAYPI .NET SDK

SDK oficial para integrar pagos QR de [TAYPI](https://taypi.pe) en aplicaciones .NET.

Acepta pagos con Yape, Plin y cualquier app bancaria conectada a la CCE.

## Compatibilidad

| Plataforma | Version |
|---|---|
| .NET Framework | 4.8+ |
| .NET | 7.0, 8.0 |
| .NET Standard | 2.0 |

## Instalacion

```bash
dotnet add package Taypi
```

O en Package Manager Console:

```powershell
Install-Package Taypi
```

## Uso rapido

```csharp
using Taypi;

using var taypi = new TaypiClient(
    "taypi_pk_test_...",  // Public key
    "taypi_sk_test_..."   // Secret key
);

// Crear sesion de checkout
var session = await taypi.CreateCheckoutSessionAsync(
    new PaymentParams
    {
        Amount = "25.00",
        Reference = "ORD-12345",
        Description = "Zapatillas Nike Air"
    },
    idempotencyKey: "ORD-12345"
);

Console.WriteLine(session["checkout_token"]);
```

### Checkout completo (C# + checkout.js)

**Backend (ASP.NET Core):**

```csharp
[HttpPost("crear-pago")]
public async Task<IActionResult> CrearPago([FromBody] OrderRequest order)
{
    var session = await _taypi.CreateCheckoutSessionAsync(
        new PaymentParams
        {
            Amount = order.Total.ToString("F2"),
            Reference = order.OrderId,
            Description = order.Description
        },
        idempotencyKey: order.OrderId
    );

    return Ok(new { checkout_token = session["checkout_token"], public_key = _taypi.PublicKey });
}
```

**Frontend:**

```html
<script src="https://app.taypi.pe/v1/checkout.js"></script>
<script>
    Taypi.publicKey = 'taypi_pk_test_...';
    Taypi.open({
        sessionToken: checkoutToken,
        onSuccess: function(result) { console.log('Pagado:', result.paid_at); },
        onExpired: function() { console.log('QR expirado'); },
        onClose: function() { console.log('Modal cerrado'); }
    });
</script>
```

## Metodos disponibles

### Checkout Sessions

```csharp
// Crear sesion para checkout.js (retorna checkout_token)
var session = await taypi.CreateCheckoutSessionAsync(
    new PaymentParams
    {
        Amount = "50.00",
        Reference = "ORD-789",
        Description = "Descripcion del pago",
        Metadata = new Dictionary<string, string> { ["source"] = "web" }
    },
    idempotencyKey: "ORD-789"
);
```

### Pagos

```csharp
// Crear pago (retorna datos completos: QR, checkout_url, etc.)
var payment = await taypi.CreatePaymentAsync(
    new PaymentParams { Amount = "50.00", Reference = "ORD-789" },
    idempotencyKey: "ORD-789"
);

// Consultar pago
var payment = await taypi.GetPaymentAsync("uuid-del-pago");

// Listar pagos con filtros
var result = await taypi.ListPaymentsAsync(new ListPaymentsFilters
{
    Status = "completed",
    From = "2026-03-01",
    To = "2026-03-31",
    PerPage = 50
});

// Cancelar pago pendiente
var cancelled = await taypi.CancelPaymentAsync("uuid-del-pago", "cancel-ORD-789");
```

### Polling (esperar pago)

Para integraciones server-to-server, POS, kioscos o cualquier caso donde no uses webhooks:

```csharp
// Crear pago
var payment = await taypi.CreatePaymentAsync(
    new PaymentParams { Amount = "15.00", Reference = "KIOSK-001" },
    idempotencyKey: "KIOSK-001"
);

// Mostrar QR al cliente...
Console.WriteLine(payment["checkout_url"]);

// Esperar hasta que pague (polling cada 3s, max 15 min)
var result = await taypi.WaitForPaymentAsync(
    payment["payment_id"]!.ToString()!,
    pollingIntervalSeconds: 3,   // cada 3 segundos (min: 1)
    timeoutSeconds: 900          // 15 minutos (coincide con TTL del QR)
);

if (result["status"]?.ToString() == "completed")
{
    Console.WriteLine("Pago recibido!");
}
```

Soporta `CancellationToken` para cancelar la espera externamente. Lanza `TimeoutException` si se supera el tiempo maximo.

### Webhooks

```csharp
// ASP.NET Core — verificar firma de webhook recibido
[HttpPost("webhooks/taypi")]
public async Task<IActionResult> HandleWebhook()
{
    using var reader = new StreamReader(Request.Body);
    var payload = await reader.ReadToEndAsync();
    var signature = Request.Headers["Taypi-Signature"].ToString();

    if (!TaypiClient.VerifyWebhook(payload, signature, _webhookSecret))
        return StatusCode(403);

    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);
    // Procesar evento: payment.completed, payment.expired, etc.
    return Ok();
}
```

**ASP.NET Framework (Web API 2):**

```csharp
[HttpPost, Route("api/webhooks/taypi")]
public async Task<IHttpActionResult> HandleWebhook()
{
    var payload = await Request.Content.ReadAsStringAsync();
    Request.Headers.TryGetValues("Taypi-Signature", out var sigValues);
    var signature = sigValues?.FirstOrDefault() ?? "";

    if (!TaypiClient.VerifyWebhook(payload, signature, webhookSecret))
        return StatusCode(HttpStatusCode.Forbidden);

    // Procesar evento...
    return Ok();
}
```

## Entornos

```csharp
// Produccion (default)
var taypi = new TaypiClient("pk", "sk");

// Desarrollo
var taypi = new TaypiClient("pk", "sk", new TaypiOptions { BaseUrl = "https://dev.taypi.pe" });

// Sandbox
var taypi = new TaypiClient("pk", "sk", new TaypiOptions { BaseUrl = "https://sandbox.taypi.pe" });
```

## Inyeccion de dependencias (ASP.NET Core)

```csharp
// Program.cs o Startup.cs
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new TaypiClient(
        config["Taypi:PublicKey"]!,
        config["Taypi:SecretKey"]!,
        new TaypiOptions { BaseUrl = config["Taypi:BaseUrl"] }
    );
});

// appsettings.json
// {
//   "Taypi": {
//     "PublicKey": "taypi_pk_test_...",
//     "SecretKey": "taypi_sk_test_...",
//     "BaseUrl": "https://dev.taypi.pe"
//   }
// }
```

### Con HttpClient personalizado (HttpClientFactory)

```csharp
builder.Services.AddHttpClient("taypi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddSingleton(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var config = sp.GetRequiredService<IConfiguration>();
    return new TaypiClient(
        config["Taypi:PublicKey"]!,
        config["Taypi:SecretKey"]!,
        new TaypiOptions { BaseUrl = config["Taypi:BaseUrl"] },
        factory.CreateClient("taypi")
    );
});
```

## Idempotencia

Todos los metodos que crean recursos (`CreateCheckoutSessionAsync`, `CreatePaymentAsync`, `CancelPaymentAsync`) requieren un `idempotencyKey` explicito. Esto protege contra pagos duplicados por reintentos de red.

```csharp
// Usar la referencia de orden como idempotency key
await taypi.CreateCheckoutSessionAsync(parameters, idempotencyKey: "ORD-12345");

// Si el mismo key se envia dentro de los 15 minutos, retorna la respuesta cacheada
// sin crear un pago nuevo.
```

## Manejo de errores

```csharp
try
{
    var session = await taypi.CreateCheckoutSessionAsync(parameters, reference);
}
catch (TaypiException ex)
{
    Console.WriteLine(ex.Message);     // "El monto minimo es S/ 1.00"
    Console.WriteLine(ex.ErrorCode);   // "PAYMENT_INVALID_AMOUNT"
    Console.WriteLine(ex.HttpCode);    // 422
    Console.WriteLine(ex.Response);    // Respuesta completa del API (Dictionary)
}
```

### Codigos de error comunes

| Codigo | HTTP | Descripcion |
|---|---|---|
| `AUTH_KEY_INVALID` | 401 | API key no existe o fue revocada |
| `AUTH_SIGNATURE_INVALID` | 403 | Firma HMAC incorrecta |
| `AUTH_TIMESTAMP_EXPIRED` | 403 | Timestamp con mas de 5 minutos |
| `RATE_LIMIT_EXCEEDED` | 429 | Demasiadas solicitudes (max 60/min) |
| `PAYMENT_INVALID_AMOUNT` | 422 | Monto fuera de rango |
| `PAYMENT_NOT_FOUND` | 404 | UUID de pago no encontrado |
| `PAYMENT_EXPIRED` | 410 | QR expiro (15 minutos) |
| `PROCESSOR_TIMEOUT` | 502 | Sistema de pagos no respondio |

## .NET Framework 4.8

El SDK configura automaticamente TLS 1.2+ en .NET Framework. No se requiere configuracion adicional.

```csharp
// Funciona igual que en .NET 7/8
var taypi = new TaypiClient("pk", "sk");
var payment = await taypi.CreatePaymentAsync(parameters, "ORD-123");
```

## Licencia

MIT - [NEO TECHNOLOGY PERU E.I.R.L.](https://neotecperu.com)
