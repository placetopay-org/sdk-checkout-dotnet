# Migration — .NET SDK: `placetopay/redirection-csharp-sdk` → `PlaceToPay.Checkout`

The legacy `redirection-csharp-sdk` targeted .NET Framework 4.5 and was never
published to NuGet. The new `PlaceToPay.Checkout` package targets .NET 10 and
has a redesigned, async-only API.

---

## TL;DR

```bash
dotnet remove package <the legacy package>        # if vendored
dotnet add package PlaceToPay.Checkout
```

Switch to async and the code is 3–5 lines shorter. The wire format is
identical, so your existing gateway configuration works as-is.

---

## 1. Target framework

| Old                            | New              |
| ------------------------------ | ---------------- |
| .NET Framework 4.5             | .NET 10.0        |

If you cannot move off .NET Framework, open an issue — we may add a
`net48` target on request.

## 2. Namespace

```diff
- using PlacetoPay.Integrations.Library.CSharp;
- using PlacetoPay.Integrations.Library.CSharp.Entities;
- using PlacetoPay.Integrations.Library.CSharp.Message;
+ using PlaceToPay.Checkout;
+ using PlaceToPay.Checkout.Models.Common;
+ using PlaceToPay.Checkout.Models.Session;
+ using PlaceToPay.Checkout.Models.Instrument;
+ using PlaceToPay.Checkout.Errors;
```

## 3. Construction

**Before**

```csharp
var gateway = new PlacetoPay("login", "trankey",
                             new Uri("https://checkout.placetopay.com/"),
                             PlacetoPay.TP_REST);
```

**After**

```csharp
var client = new CheckoutClient(new CheckoutClientOptions
{
    Login       = "login",
    SecretKey   = "trankey",           // renamed; this is the raw credential, not the digest
    Environment = Environment.Production,
    Country     = Country.Colombia,    // supplies locale + currency defaults
});
```

Key differences:

- No more `TP_REST` / `TP_SOAP` toggle. SOAP is gone; REST is the only
  supported transport.
- `SecretKey` is the merchant credential (used to compute `tranKey` on the
  wire). The old SDK's property name `trankey` was misleading because
  `tranKey` on the wire is the computed digest.
- `Environment.Sandbox` / `Environment.Production` presets replace manual
  `Uri` construction. Use `BaseUrl` only as an escape hatch.

## 4. Async everywhere

All operations are now `async Task<T>`:

```diff
- var response     = gateway.Request(new RedirectRequest {…});
- var information  = gateway.Query("12345");
- var collected    = gateway.Collect(new CollectRequest {…});
- var reversed     = gateway.Reverse("refX");
+ var response     = await client.CreateSessionAsync(req);
+ var information  = await client.QuerySessionAsync(12345);
+ var collected    = await client.CollectAsync(req);
+ var reversed     = await client.ReverseAsync("refX");
+ var canceled     = await client.CancelSessionAsync(12345);                // new
+ var invalidated  = await client.InvalidateTokenAsync(invalidateReq);      // new
```

All methods accept an optional trailing `CancellationToken ct = default`.

## 5. Method renames

| Legacy                            | New                                     |
| --------------------------------- | --------------------------------------- |
| `Request(RedirectRequest)`        | `CreateSessionAsync(CreateSessionRequest)` |
| `Query(string)`                   | `QuerySessionAsync(long)`               |
| `Collect(CollectRequest)`         | `CollectAsync(CollectRequest)`          |
| `Reverse(string)`                 | `ReverseAsync(string)`                  |
| *(not available)*                 | `CancelSessionAsync(long)` **new**      |
| *(not available)*                 | `InvalidateTokenAsync(InvalidateTokenRequest)` **new** |

## 6. The inverted-logic bug

The legacy SDK had this bug in `Transaction.IsApproved()`:

```csharp
// legacy — WRONG, returns true when status is NOT approved
return (this.status != null) && (!this.status.status.Equals(Status.ST_APPROVED));
```

If your code branched on `IsApproved()` it was running the wrong branch
continuously. Replace with:

```csharp
if (transaction.IsApproved) { … }           // strict: only StatusCode.Approved
// or
if (transaction.IsSuccessful) { … }          // permissive: OK / Approved / ApprovedPartial
```

## 7. Error handling

The legacy `PlacetoPayException` was a single catch-all. Use the typed
hierarchy:

```diff
  try
  {
-     var result = gateway.Request(req);
+     var result = await client.CreateSessionAsync(req);
  }
- catch (PlacetoPayException ex)
- {
-     Log(ex.Message);
- }
+ catch (ValidationException ex)
+ {
+     foreach (var fe in ex.FieldErrors) Log(fe.Field, fe.Message);
+ }
+ catch (AuthenticationException)            { /* bad credentials */ }
+ catch (RateLimitException ex)              { await Task.Delay(ex.RetryAfter ?? TimeSpan.FromSeconds(1)); }
+ catch (NetworkException ex)                { Log(ex.InnerException?.Message); }
```

## 8. Dependency swap

| Legacy                | New                   | Reason                                    |
| --------------------- | --------------------- | ----------------------------------------- |
| RestSharp 105.2.3     | `HttpClient` (native) | No transitive attack surface              |
| Newtonsoft.Json 10.0.3| `System.Text.Json`    | Native, faster, source-generator friendly |
| SoapHttpClient 1.4.3  | *(removed)*           | SOAP transport dropped                    |

## 9. DI / `IHttpClientFactory`

If you use `Microsoft.Extensions.DependencyInjection`, wire the client
through `IHttpClientFactory`:

```csharp
builder.Services.AddHttpClient("placetopay");
builder.Services.AddSingleton<ICheckoutClient>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("placetopay");
    return new CheckoutClient(new CheckoutClientOptions
    {
        Login       = builder.Configuration["PlaceToPay:Login"]!,
        SecretKey   = builder.Configuration["PlaceToPay:SecretKey"]!,
        Environment = Environment.Sandbox,
        HttpClient  = http,                  // injected handler chain
        Logger      = sp.GetRequiredService<ILogger<CheckoutClient>>(),
    });
});
```

Use `dotnet user-secrets` for local development.

## 10. Security improvements you get for free

- **SHA-256 signing** (legacy SDK used SHA-1, cryptographically broken).
- **Cryptographic nonce** via `RandomNumberGenerator.GetBytes` (legacy SDK
  used `System.Random`).
- **HTTPS enforcement** — plain `http://` rejected at startup unless it
  targets localhost for tests.
- **PCI/PII log redaction** — `RedactingJsonLogger` scrubs PAN, CVV,
  `tranKey`, `nonce`, tokens, pin, password at DEBUG level.

## 11. New features

- `CancelSessionAsync` — stop a pending session.
- `InvalidateTokenAsync` — revoke a stored token.
- `CheckoutClientOptions.AuthAdditional` — merchant-specific metadata on
  the auth block.
- `CollectRequest.Provider` — optional processor override.
- `Discount` record modeled explicitly.
- `Status.IsApproved / IsRejected / IsError` convenience properties.
- `SessionInformation.LastTransaction(bool) / LastApprovedTransaction /
  LastAuthorization()` convenience helpers.
- Typed `Autopay` request support for recurring-with-token flows.
