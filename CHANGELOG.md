# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- `QuerySessionAsync` / `CollectAsync` now tolerate heterogeneous
  `payment[].processorFields[].value` payloads (string, number, boolean, object)
  to match gateway responses without deserialization errors.

## [1.0.0] - 2026-04-24

### Added

- First public release of the `PlaceToPay.Checkout` .NET SDK.
- `CheckoutClient` / `ICheckoutClient` with async-only API covering all six
  endpoints of the Web Checkout REST API:
  - `CreateSessionAsync`
  - `QuerySessionAsync`
  - `CancelSessionAsync` (new — missing in the legacy `redirection-csharp-sdk`)
  - `CollectAsync`
  - `ReverseAsync`
  - `InvalidateTokenAsync`
- `Environment` preset enum (`Sandbox` / `Production`) and `Country` preset
  enum (Colombia, Ecuador, Panama, Costa Rica) composing base URL + default
  locale + default currency.
- SHA-256 authentication with a cryptographically-random 16-byte nonce
  (`RandomNumberGenerator.GetBytes`). `System.Random` and SHA-1 are gone.
- HTTP transport built on `HttpClient` (native) with:
  - Connect timeout 10s, read timeout 30s (configurable).
  - Exponential-backoff retry with jitter for network errors, 5xx and 429.
  - `Retry-After` support (seconds or HTTP-date).
  - `X-Idempotency-Key` propagation on `createSession`, `collect`, `reverse`.
- Typed exception hierarchy: `PlaceToPayException`, `AuthenticationException`,
  `ValidationException`, `NotFoundException`, `RateLimitException`,
  `NetworkException`, `ServerException`.
- `RedactingJsonLogger` that masks `tranKey`, `nonce`, `cvv`, `pin`,
  `password`, card PAN, token values and `secretKey` from DEBUG-level logs.
- `NotificationVerifier` with `VerifySha256` (primary) and
  `VerifySha1Legacy` (interop with the 2018-era Java SDK).
- `IServiceCollection.AddPlaceToPayCheckout(...)` extension for DI wiring
  with full `IHttpClientFactory` integration.
- `PlaceToPay.Checkout.WebExample` ASP.NET Core Minimal API app that
  exercises every endpoint against the sandbox.

### Removed (vs legacy `redirection-csharp-sdk`)

- SOAP transport.
- Dependency on `RestSharp` and `Newtonsoft.Json`.
- SHA-1 signing.
- `System.Random` for nonce generation.
- The inverted-logic bug in `Transaction.IsApproved()`.

[1.0.0]: https://github.com/placetopay/checkout-dotnet/releases/tag/v1.0.0
