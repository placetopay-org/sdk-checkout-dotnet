# PlaceToPay Checkout - SDK .NET

[![NuGet](https://img.shields.io/nuget/v/PlaceToPay.Checkout.svg)](https://www.nuget.org/packages/PlaceToPay.Checkout/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

SDK oficial de .NET 10 para **PlaceToPay Web Checkout**.

Esta libreria te permite integrar pagos con una API simple, asincrona y lista
para produccion: creas la sesion, rediriges al comprador y consultas el estado
final de la transaccion.

Terminología estándar del proyecto: ver `../docs/GLOSARIO.md`.

> Si vienes del SDK legacy de C#, revisa `MIGRATION.md` para mapear metodos y modelos.

---

## Instalación

```bash
dotnet add package PlaceToPay.Checkout
```

Objetivo: **.NET 10** (LTS, noviembre 2025).

## Versiones soportadas

Seguimos SemVer y el ciclo de soporte de .NET.

| Versión SDK | Target framework | Estado | Parches de seguridad hasta |
|-------------|------------------|--------|----------------------------|
| `1.x` | `net10.0` (LTS) | **Actual** | Alineado al EOL de .NET 10 (noviembre 2028) |
| `0.x` | - | Pre-release | End-of-life |

**Política**

- Soportamos el .NET LTS actual.
- Cambios breaking solo en versiones major.
- .NET Framework y .NET Standard no son soportados.
- La ruta de migración desde legacy está en `MIGRATION.md`.

## Quickstart (5 minutos)

En menos de 5 minutos puedes crear una sesion de pago y obtener el `processUrl`
para redirigir al comprador.

```csharp
using PlaceToPay.Checkout;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Session;

var client = new CheckoutClient(new CheckoutClientOptions
{
    Login       = "YOUR_LOGIN",
    SecretKey   = "YOUR_SECRET_KEY",
    Environment = Environment.Sandbox,
    Country     = Country.Colombia,
});

var response = await client.CreateSessionAsync(new CreateSessionRequest
{
    IpAddress = "127.0.0.1",
    UserAgent = "MyApp/1.0",
    ReturnUrl = "https://example.com/return",
    Payment = new Payment
    {
        Reference   = "ORDER-0001",
        Description = "Demo purchase",
        Amount      = new Amount { Currency = "COP", Total = 50_000m },
    },
});

Console.WriteLine(response.ProcessUrl);
Console.WriteLine(response.RequestId);
```

Después del pago:

```csharp
var info = await client.QuerySessionAsync(response.RequestId);
if (info.Status?.Status == StatusCode.Approved)
{
    // completar orden
}
```

## Que puedes hacer con este SDK

| Endpoint | Método | Notas |
| --- | --- | --- |
| `POST /api/session` | `CreateSessionAsync` | Soporta `autopay`, `metadata`, `dispersion` |
| `POST /api/session/{id}` | `QuerySessionAsync` | |
| `POST /api/session/{id}/cancel` | `CancelSessionAsync` | Nuevo vs SDK legacy |
| `POST /api/collect` | `CollectAsync` | Cobro con instrumento tokenizado |
| `POST /api/reverse` | `ReverseAsync` | Por `internalReference` |
| `POST /api/instrument/invalidate` | `InvalidateTokenAsync` | Revoca token |
| Verificación webhook SHA-256 | `NotificationVerifier.VerifySha256` | Recomendado |
| Verificación webhook SHA-1 | `NotificationVerifier.VerifySha1Legacy` | Solo compatibilidad legacy |

## Flujo de integracion recomendado

Flujo de sesion (checkout web):

1. `CreateSessionAsync` para iniciar el checkout y obtener `requestId` + `processUrl`.
2. `QuerySessionAsync` para confirmar estado despues del retorno del pagador.
3. `CancelSessionAsync` para invalidar una sesion pendiente cuando aplica.

Flujo de tokenizacion y post-pago:

1. `CollectAsync` para cobrar con instrumento tokenizado.
2. `ReverseAsync` para reversar por `internalReference`.
3. `InvalidateTokenAsync` para revocar el token.

## Funcionalidades incluidas en v1

- Soporte de `auth.additional` desde `CheckoutClientOptions.AuthAdditional`.
- Soporte de `CollectRequest.Provider` para override opcional de procesador.
- Helpers de estado en `Status` y `Transaction` (`IsApproved`, `IsRejected`, `IsError`, `IsSuccessful`).
- Helpers de sesion en `SessionInformation` (`LastTransaction`, `LastApprovedTransaction`, `LastAuthorization`).
- Compatibilidad con valores heterogeneos en `processorFields[].value`.

## Casos de uso comunes

- Checkout estandar en web con redireccion (`CreateSessionAsync` + `QuerySessionAsync`).
- Cobro con token guardado (`CollectAsync`) para compras recurrentes.
- Reversion de pagos (`ReverseAsync`) y revocacion de token (`InvalidateTokenAsync`).

### Limitaciones por pais

- Ecuador: la funcionalidad `autopay` no esta habilitada en WebCheckout para este pais. Si se envia en `CreateSessionAsync`, puede ser rechazada por el gateway.
- Ecuador: la recurrencia en pago unico no esta disponible actualmente.

### Compatibilidad de respuestas del gateway

- En respuestas de `querySession` / `collect`, `payment[].processorFields[].value` puede llegar como `string`, `number`, `boolean` u objeto JSON.
- El SDK modela ese campo como valor heterogeneo para evitar errores de deserializacion cuando el adquirente envia metadatos no-string.

## Configuración

`CheckoutClientOptions`:

| Propiedad | Tipo | Default | Uso |
| --- | --- | --- | --- |
| `Login` | `string` | requerido | Login de comercio |
| `SecretKey` | `string` | requerido | Secret del comercio |
| `Environment` | `Environment` | - | `Sandbox` o `Production` |
| `BaseUrl` | `Uri` | - | Override explícito (solo HTTPS) |
| `Country` | `Country` | `null` | `Colombia`, `Ecuador`, etc |
| `Timeout` | `TimeSpan` | 30s | Timeout de lectura |
| `ConnectTimeout` | `TimeSpan` | 10s | Timeout de conexión |
| `MaxRetryAttempts` | `int` | 3 | 1 = sin retries |
| `InitialRetryDelay` | `TimeSpan` | 200ms | Backoff inicial |
| `MaxRetryDelay` | `TimeSpan` | 5s | Tope de backoff |
| `HttpClient` | `HttpClient` | SDK-owned | Inyección por DI/proxy/tracing |
| `Logger` | `ILogger` | `NullLogger` | DEBUG con payload redactado |
| `UserAgentSuffix` | `string` | `null` | Sufijo de user-agent |
| `AdditionalHeaders` | `IReadOnlyDictionary<...>` | `null` | Headers extra |

## Manejo de errores

Todas las excepciones heredan de `PlaceToPayException`:

- `AuthenticationException`
- `ValidationException`
- `NotFoundException`
- `RateLimitException`
- `NetworkException`
- `ServerException`

## Verificación de webhook

```csharp
using PlaceToPay.Checkout.Webhooks;

app.MapPost("/placetopay/webhook", async (NotificationPayload payload, string secret) =>
{
    if (!NotificationVerifier.VerifySha256(payload, secret))
    {
        return Results.Unauthorized();
    }

    return Results.Ok();
});
```

> Las notificaciones webhook de PlaceToPay están confirmadas sobre SHA-256.

## Integración con DI

```csharp
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ICheckoutClient>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("placetopay");
    return new CheckoutClient(new CheckoutClientOptions
    {
        Login       = builder.Configuration["PlaceToPay:Login"]!,
        SecretKey   = builder.Configuration["PlaceToPay:SecretKey"]!,
        Environment = Environment.Sandbox,
        Country     = Country.Colombia,
        HttpClient  = http,
        Logger      = sp.GetRequiredService<ILogger<CheckoutClient>>(),
    });
});
```

## Pruebas locales con app de ejemplo

`examples/PlaceToPay.Checkout.WebExample/` es una app Minimal API para probar todos los flujos. Ver [`examples/README.md`](examples/PlaceToPay.Checkout.WebExample/README.md).

```bash
cd examples/PlaceToPay.Checkout.WebExample
dotnet user-secrets set "PlaceToPay:Login"     "YOUR_LOGIN"
dotnet user-secrets set "PlaceToPay:SecretKey" "YOUR_SECRET"
dotnet run
# http://localhost:5000
```

## Seguridad

- Firma: SHA-256.
- Nonce: 16 bytes criptográficos con `RandomNumberGenerator.GetBytes`.
- TLS: HTTPS obligatorio.
- Logging: redacción automática de PAN, CVV, `tranKey`, `nonce`, token, PIN y password.
- Disclosure: ver [SECURITY.md](SECURITY.md).

## Licencia

MIT, ver [LICENSE](LICENSE).
