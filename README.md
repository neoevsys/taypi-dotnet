# TAYPI .NET SDK

SDK oficial para integrar pagos QR de [TAYPI](https://taypi.pe) en aplicaciones .NET.

Acepta pagos con Yape, Plin y cualquier app bancaria conectada a la CCE.

## Compatibilidad

| Plataforma | Version |
|---|---|
| .NET Framework | 4.8+ (WinForms, WPF, ASP.NET) |
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

// El SDK detecta automaticamente el ambiente desde el prefijo del key:
//   taypi_pk_test_... → sandbox (sandbox.taypi.pe)
//   taypi_pk_live_... → produccion (app.taypi.pe)
using var taypi = new TaypiClient(
    "taypi_pk_test_...",  // Public key
    "taypi_sk_test_..."   // Secret key — nunca se envia por la red
);

// Crear pago con QR
PaymentResponse payment = await taypi.CreatePaymentAsync(
    new PaymentParams
    {
        Amount = "25.00",
        Reference = "ORD-12345",
        Description = "Zapatillas Nike Air"
    },
    idempotencyKey: "ORD-12345"
);

Console.WriteLine(payment.PaymentId);   // UUID del pago
Console.WriteLine(payment.QrImage);     // imagen SVG base64 lista para mostrar
Console.WriteLine(payment.QrText);      // texto del QR (para generar imagen propia)
Console.WriteLine(payment.CheckoutUrl); // URL de checkout
Console.WriteLine(payment.ExpiresAt);   // fecha de expiracion
```

## Dos flujos de integracion

### 1. CreatePaymentAsync (WinForms, POS, server-to-server)

Devuelve todo en un solo paso: QR, URL, payment_id. **Este es el recomendado para aplicaciones de escritorio.**

```csharp
PaymentResponse payment = await taypi.CreatePaymentAsync(
    new PaymentParams
    {
        Amount = "50.00",
        Reference = "VENTA-001",
        Description = "Venta en tienda"
    },
    idempotencyKey: "VENTA-001"
);

// Propiedades disponibles:
payment.PaymentId       // "a1515df6-92d4-..."
payment.Amount          // "50.00"
payment.Currency        // "PEN"
payment.Status          // "pending"
payment.QrText          // "0002010102122637..." (texto para generar QR propio)
payment.QrImage         // "data:image/svg+xml;base64,..." (imagen lista para mostrar)
payment.CheckoutUrl     // "https://sandbox.taypi.pe/qr/cRP2tzLzBW"
payment.CheckoutToken   // UUID de sesion
payment.ShortHash       // "cRP2tzLzBW"
payment.ExpiresAt       // "2026-03-17T19:46:48-05:00"
payment.CreatedAt       // "2026-03-16T19:46:48-05:00"
payment.Description     // "Venta en tienda"
```

### 2. Checkout Sessions (web con checkout.js)

Flujo de dos pasos para integraciones web con el widget JavaScript.

**Backend (ASP.NET Core):**

```csharp
[HttpPost("crear-pago")]
public async Task<IActionResult> CrearPago([FromBody] OrderRequest order)
{
    // Paso 1: crear sesion (retorna solo checkout_token)
    CheckoutSessionResponse session = await _taypi.CreateCheckoutSessionAsync(
        new PaymentParams
        {
            Amount = order.Total.ToString("F2"),
            Reference = order.OrderId,
            Description = order.Description
        },
        idempotencyKey: order.OrderId
    );

    return Ok(new { checkout_token = session.CheckoutToken, public_key = _taypi.PublicKey });
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

**Consultar datos del QR (opcional):**

```csharp
// Paso 2: obtener datos completos de la sesion
CheckoutSessionDetail detail = await taypi.GetCheckoutSessionAsync(session.CheckoutToken);

detail.PaymentId      // UUID del pago
detail.QrImage        // imagen SVG base64
detail.Amount         // "50.00"
detail.Status         // "pending"
detail.MerchantName   // nombre del comercio
detail.StoreName      // nombre de la tienda/sucursal
detail.ExpiresAt      // fecha de expiracion
```

## Campos QR: QrText vs QrImage

| Campo | Que es | Para que sirve |
|---|---|---|
| `QrText` | `"0002010102122637..."` texto EMVCo | Generar tu propio QR con ZXing, QRCoder, etc. |
| `QrImage` | `"data:image/svg+xml;base64,PD9..."` imagen SVG | Mostrar directamente como imagen |
| `CheckoutUrl` | `"https://sandbox.taypi.pe/qr/xxx"` URL | Abrir en navegador |

## Metodos disponibles

### Pagos

```csharp
// Crear pago con QR (todo en un paso)
PaymentResponse payment = await taypi.CreatePaymentAsync(params, idempotencyKey);

// Consultar pago
PaymentResponse payment = await taypi.GetPaymentAsync("uuid-del-pago");

// Listar pagos con filtros
PaymentListResponse list = await taypi.ListPaymentsAsync(new ListPaymentsFilters
{
    Status = "completed",
    From = "2026-03-01",
    To = "2026-03-31",
    PerPage = 50
});
// list.Data → List<PaymentResponse>
// list.Meta.Total, list.Meta.CurrentPage, list.Meta.LastPage

// Cancelar pago pendiente
PaymentResponse cancelled = await taypi.CancelPaymentAsync("uuid", "cancel-ORD-789");
```

### Polling (esperar pago)

Para POS, kioscos o cualquier caso sin webhooks:

```csharp
PaymentResponse payment = await taypi.CreatePaymentAsync(
    new PaymentParams { Amount = "15.00", Reference = "KIOSK-001" },
    idempotencyKey: "KIOSK-001"
);

// Mostrar QR al cliente...
Console.WriteLine(payment.CheckoutUrl);

// Esperar hasta que pague (polling cada 3s, max 15 min)
PaymentResponse result = await taypi.WaitForPaymentAsync(
    payment.PaymentId,
    pollingIntervalSeconds: 3,
    timeoutSeconds: 900
);

if (result.Status == "completed")
    Console.WriteLine($"Pago recibido! Pagador: {result.PayerName}");
```

Soporta `CancellationToken` para cancelar la espera externamente. Lanza `TimeoutException` si se supera el tiempo maximo.

### Checkout Sessions

```csharp
// Crear sesion (retorna solo checkout_token)
CheckoutSessionResponse session = await taypi.CreateCheckoutSessionAsync(params, idempotencyKey);

// Consultar datos completos de la sesion (QR, monto, merchant, etc.)
CheckoutSessionDetail detail = await taypi.GetCheckoutSessionAsync(session.CheckoutToken);
```

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

El SDK detecta automaticamente el ambiente desde el prefijo de las API keys:

```csharp
// Keys de test → sandbox automatico
var taypi = new TaypiClient("taypi_pk_test_...", "taypi_sk_test_...");
Console.WriteLine(taypi.IsSandbox); // true

// Keys de produccion → produccion automatico
var taypi = new TaypiClient("taypi_pk_live_...", "taypi_sk_live_...");
Console.WriteLine(taypi.IsSandbox); // false
```

No necesitas configurar `BaseUrl` manualmente. El SDK valida que:
- Ambas keys sean del mismo ambiente (test + test o live + live)
- Si configuras `BaseUrl` explicitamente, sea consistente con las keys

## Validacion de API keys

El SDK valida las keys al instanciar el cliente:

```csharp
// Key con formato invalido → TaypiException "INVALID_KEY_FORMAT"
new TaypiClient("pk_invalida", "sk_invalida");

// Keys de ambientes diferentes → TaypiException "KEY_ENVIRONMENT_MISMATCH"
new TaypiClient("taypi_pk_test_...", "taypi_sk_live_...");

// Key de test con URL de produccion → TaypiException "KEY_URL_MISMATCH"
new TaypiClient("taypi_pk_test_...", "taypi_sk_test_...",
    new TaypiOptions { BaseUrl = "https://app.taypi.pe" });
```

## Inyeccion de dependencias (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new TaypiClient(
        config["Taypi:PublicKey"]!,
        config["Taypi:SecretKey"]!
    );
});

// appsettings.json
// {
//   "Taypi": {
//     "PublicKey": "taypi_pk_test_...",
//     "SecretKey": "taypi_sk_test_..."
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
        new TaypiOptions(),
        factory.CreateClient("taypi")
    );
});
```

## Idempotencia

Todos los metodos que crean recursos requieren un `idempotencyKey` explicito. Esto protege contra pagos duplicados por reintentos de red.

```csharp
// Usar la referencia de orden como idempotency key
await taypi.CreatePaymentAsync(parameters, idempotencyKey: "ORD-12345");

// Si el mismo key se envia dentro de las 24 horas, retorna la respuesta cacheada
// sin crear un pago nuevo.
```

## Manejo de errores

```csharp
try
{
    var payment = await taypi.CreatePaymentAsync(parameters, reference);
}
catch (TaypiException ex)
{
    Console.WriteLine(ex.Message);     // "El monto minimo es S/ 1.00"
    Console.WriteLine(ex.ErrorCode);   // "PAYMENT_INVALID_AMOUNT"
    Console.WriteLine(ex.HttpCode);    // 422
}
```

### Codigos de error comunes

| Codigo | HTTP | Descripcion |
|---|---|---|
| `INVALID_KEY_FORMAT` | — | Key mal formada (se lanza al construir el cliente) |
| `KEY_ENVIRONMENT_MISMATCH` | — | Una key es test y otra live |
| `KEY_URL_MISMATCH` | — | Key no coincide con la URL configurada |
| `AUTH_KEY_INVALID` | 401 | API key no existe o fue revocada |
| `AUTH_SIGNATURE_INVALID` | 403 | Firma HMAC incorrecta |
| `AUTH_TIMESTAMP_EXPIRED` | 403 | Timestamp con mas de 5 minutos |
| `RATE_LIMIT_EXCEEDED` | 429 | Demasiadas solicitudes (max 60/min) |
| `PAYMENT_INVALID_AMOUNT` | 422 | Monto fuera de rango |
| `PAYMENT_NOT_FOUND` | 404 | UUID de pago no encontrado |
| `PAYMENT_EXPIRED` | 410 | QR expiro (15 minutos) |
| `PROCESSOR_TIMEOUT` | 502 | Sistema de pagos no respondio |
| `TIMEOUT` | — | Timeout de conexion HTTP |
| `CONNECTION_ERROR` | — | Error de red |

## Seguridad

- El **secret key** nunca se envia por la red. Solo se usa localmente para calcular la firma HMAC-SHA256.
- Cada request se firma automaticamente: `HMAC-SHA256(secret, timestamp + method + path + body)`
- El header `Authorization: Bearer` lleva la **public key**, no la secret.
- TLS 1.2+ se configura automaticamente en .NET Framework 4.8.

## .NET Framework 4.8

El SDK es compatible con .NET Framework 4.8 sin configuracion adicional:

```csharp
// Funciona igual que en .NET 7/8
var taypi = new TaypiClient("taypi_pk_test_...", "taypi_sk_test_...");
PaymentResponse payment = await taypi.CreatePaymentAsync(parameters, "ORD-123");
```

## Licencia

MIT - [NEO TECHNOLOGY PERU E.I.R.L.](https://neotecperu.com)
