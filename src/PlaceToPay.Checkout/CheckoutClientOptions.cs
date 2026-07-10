using System.Net.Http;
using Microsoft.Extensions.Logging;
using PlaceToPay.Checkout.Internal;

namespace PlaceToPay.Checkout;

/// <summary>
/// Configuration for a <see cref="CheckoutClient"/> instance. Pass via constructor.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="Environment"/> or <see cref="BaseUrl"/> must be supplied.
/// If both are supplied, <see cref="BaseUrl"/> wins (escape hatch).
/// </remarks>
public sealed record CheckoutClientOptions
{
    /// <summary>Merchant login identifier. Required.</summary>
    public required string Login { get; init; }

    /// <summary>Merchant secret key. Required. NEVER transmitted, never logged.</summary>
    public required string SecretKey { get; init; }

    /// <summary>Preset environment (sandbox / production). Mutually exclusive with <see cref="BaseUrl"/>.</summary>
    public Environment? Environment { get; init; }

    /// <summary>Explicit base URL (e.g. a Country-specific domain). Wins over <see cref="Environment"/>.</summary>
    public Uri? BaseUrl { get; init; }

    /// <summary>Country preset — supplies default locale + currency + optional baseUrl override.</summary>
    public Country? Country { get; init; }

    /// <summary>Full read timeout. Default 30s.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Connection timeout. Default 10s.</summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Maximum number of total attempts (1 initial + N-1 retries). Default 3.</summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>Initial retry delay (before exponential backoff). Default 200 ms.</summary>
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromMilliseconds(200);

    /// <summary>Maximum retry delay between attempts. Default 5 s.</summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Optional pre-built <see cref="HttpClient"/>. Use for custom handlers (tracing,
    /// proxy, <c>IHttpClientFactory</c> integration). If <see langword="null"/> the
    /// SDK constructs its own one-shot client.
    /// </summary>
    public HttpClient? HttpClient { get; init; }

    /// <summary>Optional structured logger. Default is <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger"/>.</summary>
    public ILogger? Logger { get; init; }

    /// <summary>Optional User-Agent suffix appended after the SDK identifier.</summary>
    public string? UserAgentSuffix { get; init; }

    /// <summary>Additional headers sent with every request.</summary>
    public IReadOnlyDictionary<string, string>? AdditionalHeaders { get; init; }

    /// <summary>
    /// Optional map transmitted inside the auth block as <c>additional</c>. Mirrors the
    /// PHP SDK's <c>authAdditional</c> option — merchant-specific metadata recorded
    /// alongside each authenticated session. NOT part of the signing digest.
    /// </summary>
    public IReadOnlyDictionary<string, string>? AuthAdditional { get; init; }

    /// <summary>
    /// Resolves and validates the effective base URL from <see cref="BaseUrl"/> and
    /// <see cref="Environment"/> + <see cref="Country"/>. Throws <see cref="InvalidOperationException"/>
    /// if the configuration is inconsistent.
    /// </summary>
    public Uri ResolveBaseUrl()
    {
        if (BaseUrl is not null)
        {
            ValidateHttps(BaseUrl);
            return BaseUrl;
        }

        if (Environment is null)
        {
            throw new InvalidOperationException(
                "CheckoutClientOptions requires exactly one of Environment or BaseUrl.");
        }

        // Country override (e.g. Ecuador) may apply in production and/or sandbox.
        if (Country is { } country)
        {
            var preset = CountryPresets.For(country);
            var countryUrl = Environment == PlaceToPay.Checkout.Environment.Production
                ? preset.ProductionBaseUrl
                : preset.SandboxBaseUrl;
            if (countryUrl is not null)
            {
                return countryUrl;
            }
        }

        return Environment switch
        {
            PlaceToPay.Checkout.Environment.Sandbox => new Uri("https://checkout-test.placetopay.com"),
            PlaceToPay.Checkout.Environment.Production => new Uri("https://checkout.placetopay.com"),
            _ => throw new InvalidOperationException($"Unknown Environment value: {Environment}"),
        };
    }

    /// <summary>
    /// Default locale derived from <see cref="Country"/>, or <see langword="null"/>
    /// if no country preset is configured.
    /// </summary>
    public string? DefaultLocale => Country is { } c ? CountryPresets.For(c).Locale : null;

    /// <summary>
    /// Default currency derived from <see cref="Country"/>, or <see langword="null"/>
    /// if no country preset is configured.
    /// </summary>
    public string? DefaultCurrency => Country is { } c ? CountryPresets.For(c).Currency : null;

    /// <summary>
    /// Run all invariants. Throws <see cref="InvalidOperationException"/> on failure.
    /// Called by <see cref="CheckoutClient"/>'s constructor.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Login))
        {
            throw new InvalidOperationException("CheckoutClientOptions.Login is required.");
        }
        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException("CheckoutClientOptions.SecretKey is required.");
        }
        if (Environment is null && BaseUrl is null)
        {
            throw new InvalidOperationException(
                "CheckoutClientOptions requires exactly one of Environment or BaseUrl.");
        }
        _ = ResolveBaseUrl();

        if (MaxRetryAttempts < 1)
        {
            throw new InvalidOperationException("MaxRetryAttempts must be >= 1.");
        }
        if (Timeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Timeout must be positive.");
        }
        if (ConnectTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("ConnectTimeout must be positive.");
        }
        if (InitialRetryDelay < TimeSpan.Zero)
        {
            throw new InvalidOperationException("InitialRetryDelay must be non-negative.");
        }
        if (MaxRetryDelay < InitialRetryDelay)
        {
            throw new InvalidOperationException("MaxRetryDelay must be >= InitialRetryDelay.");
        }
    }

    private static void ValidateHttps(Uri uri)
    {
        if (!uri.IsAbsoluteUri || !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Base URL must be an absolute HTTPS URL, got '{uri}'.");
        }
    }
}
