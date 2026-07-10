# Contributing — PlaceToPay Checkout .NET SDK

Thanks for taking the time to contribute. This document walks through local
setup, the expected workflow for changes, and the quality bar we enforce.

## Local setup

```bash
git clone https://github.com/placetopay/checkout-dotnet.git
cd checkout-dotnet
dotnet restore
```

The SDK targets **.NET 10.0** (see `Directory.Build.props`). Install the .NET
10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0.

## Running the test suite

```bash
dotnet build                                             # restore + compile
dotnet test                                              # all tests
dotnet test --collect:"XPlat Code Coverage"              # with coverlet
dotnet test --settings coverlet.runsettings              # enforces ≥ 85 %
```

CI runs the last invocation. Any PR that drops coverage below 85 % fails
the build.

## Running the examples app

```bash
cd examples/PlaceToPay.Checkout.WebExample
dotnet user-secrets set "PlaceToPay:Login"     "YOUR_LOGIN"
dotnet user-secrets set "PlaceToPay:SecretKey" "YOUR_SECRET"
dotnet run                                               # http://localhost:5000
```

## Coding standards

- `Directory.Build.props` sets `Nullable=enable` and
  `TreatWarningsAsErrors=true`. Suppressions must carry a justification
  comment.
- **`System.Text.Json` only.** We do not take a Newtonsoft dependency.
- **`HttpClient` only.** We do not take a RestSharp / Refit dependency.
- Async-only public API. Every method returns `Task<T>` and accepts a
  `CancellationToken` with a default of `default`.
- Library code uses `.ConfigureAwait(false)` on all `await`s.
- Follow the existing folder/namespace convention
  (`PlaceToPay.Checkout.<Area>`). `internal` anything that isn't part of
  the public SDK surface; expose to tests via
  `[assembly: InternalsVisibleTo("PlaceToPay.Checkout.Tests")]`.

## Logs and PCI/PII

All sensitive fields are redacted by `RedactingJsonLogger`. Before adding
a new field that might carry PAN, CVV, token, pin or password content,
add the property name to the redaction sets. Unit-test the redaction
behavior in `test/.../Http/RedactingJsonLoggerTests.cs`.

## The cross-SDK auth test vector

If you touch `AuthSigner.cs`, `AuthSignerTests` must continue to produce
identical output to the Python and Java SDKs for these inputs:

```
login     = "SDK_TEST_LOGIN"
secretKey = "SDK_TEST_SECRET"
rawNonce  = Convert.FromHexString("00112233445566778899aabbccddeeff")
seed      = "2026-04-22T15:30:45+00:00"

→ Nonce   = "ABEiM0RVZneImaq7zN3u/w=="
→ TranKey = "ktUB+Efbix9v8H/DxdxtDQu9u33E4BLGr6so+LrvAjs="
```

Any change in that output is a **cross-SDK contract break** and must be
discussed + mirrored in `checkout-python` and `checkout-java` before merging.

## Pull requests

1. Branch off `main`.
2. Open a PR with a short description of **why** the change is needed.
3. `dotnet build` and `dotnet test --settings coverlet.runsettings` must
   pass locally before requesting review.
4. Add tests for new behavior. `ModelRoundtripTests.cs` is the right place
   for new public records/DTOs.
5. Update `CHANGELOG.md` under an `Unreleased` section (Keep a Changelog
   format).
6. Keep the auth test vector green.

## Reporting issues

GitHub issue tracker for bugs and feature requests. For **security issues**,
do NOT open a public issue — email security@placetopay.com per
[SECURITY.md](../SECURITY.md).

## License

By contributing you agree that your changes are licensed under the MIT
license (see [LICENSE](LICENSE)).
