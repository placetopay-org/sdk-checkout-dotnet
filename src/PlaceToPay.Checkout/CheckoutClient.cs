using Microsoft.Extensions.Logging;
using PlaceToPay.Checkout.Http;
using PlaceToPay.Checkout.Internal;
using PlaceToPay.Checkout.Models.Collect;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Instrument;
using PlaceToPay.Checkout.Models.Reverse;
using PlaceToPay.Checkout.Models.Session;

namespace PlaceToPay.Checkout;

/// <summary>
/// Default implementation of <see cref="ICheckoutClient"/>. Thread-safe — construct
/// once and share.
/// </summary>
/// <example>
/// <code>
/// var client = new CheckoutClient(new CheckoutClientOptions
/// {
///     Login = "YOUR_MERCHANT_LOGIN",
///     SecretKey = "YOUR_SECRET_KEY",
///     Environment = Environment.Sandbox,
///     Country = Country.Colombia,
/// });
///
/// var response = await client.CreateSessionAsync(new CreateSessionRequest
/// {
///     IpAddress = "127.0.0.1",
///     UserAgent = "MyApp/1.0",
///     ReturnUrl  = "https://example.com/return",
///     Payment = new Payment
///     {
///         Reference   = "order-123",
///         Description = "Demo purchase",
///         Amount      = new Amount { Currency = "COP", Total = 50_000m },
///     },
/// });
///
/// Console.WriteLine(response.ProcessUrl);
/// </code>
/// </example>
public sealed class CheckoutClient : ICheckoutClient, IDisposable
{
    private readonly HttpTransport _transport;
    private readonly CheckoutClientOptions _options;

    /// <summary>
    /// Constructs a client using the logger configured on
    /// <paramref name="options"/> (or a no-op logger if none is set).
    /// </summary>
    /// <param name="options">Validated client options. Throws
    /// <see cref="InvalidOperationException"/> if required fields are missing.</param>
    public CheckoutClient(CheckoutClientOptions options)
        : this(options, options?.Logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance)
    {
    }

    /// <summary>
    /// Constructs a client with an explicit logger. Prefer this overload when
    /// integrating with <c>Microsoft.Extensions.DependencyInjection</c> so the
    /// SDK shares your application's logging pipeline.
    /// </summary>
    /// <param name="options">Validated client options.</param>
    /// <param name="logger">Logger receiving DEBUG-level redacted request/response traces.</param>
    public CheckoutClient(CheckoutClientOptions options, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _transport = new HttpTransport(options, logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
    }

    /// <inheritdoc/>
    public CheckoutClientOptions Options => _options;

    /// <inheritdoc/>
    public Task<CreateSessionResponse> CreateSessionAsync(
        CreateSessionRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var withDefaults = ApplyCountryDefaults(request);
        return _transport.PostAsync<CreateSessionResponse>(
            "/api/session", withDefaults, idempotencyKey, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<SessionInformation> QuerySessionAsync(long requestId, CancellationToken cancellationToken = default)
    {
        return _transport.PostAuthOnlyAsync<SessionInformation>(
            $"/api/session/{requestId}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<CancelSessionResponse> CancelSessionAsync(long requestId, CancellationToken cancellationToken = default)
    {
        return _transport.PostAuthOnlyAsync<CancelSessionResponse>(
            $"/api/session/{requestId}/cancel", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<SessionInformation> CollectAsync(
        CollectRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _transport.PostAsync<SessionInformation>(
            "/api/collect", request, idempotencyKey, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ReverseResponse> ReverseAsync(
        string internalReference,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(internalReference);
        var body = new ReverseRequest { InternalReference = internalReference };
        return _transport.PostAsync<ReverseResponse>(
            "/api/reverse", body, idempotencyKey, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Status> InvalidateTokenAsync(
        InvalidateTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Server returns { "status": {...} } — wrap in ErrorResponse-shaped envelope.
        var envelope = await _transport.PostAsync<StatusEnvelope>(
            "/api/instrument/invalidate", request, null, cancellationToken).ConfigureAwait(false);
        return envelope?.Status ?? throw new InvalidOperationException("Server returned no status for invalidateToken.");
    }

    /// <summary>
    /// Releases the underlying <see cref="HttpClient"/>. Safe to call multiple
    /// times. Once disposed, the client cannot be reused.
    /// </summary>
    public void Dispose()
    {
        _transport.Dispose();
    }

    // ─── Internal helpers ────────────────────────────────────────────────

    private CreateSessionRequest ApplyCountryDefaults(CreateSessionRequest request)
    {
        if (_options.Country is null)
        {
            return request;
        }

        var preset = CountryPresets.For(_options.Country.Value);
        var locale = request.Locale ?? preset.Locale;

        // Records with required init-only properties — use `with` to copy.
        return request with { Locale = locale };
    }

    private sealed record StatusEnvelope
    {
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public Status? Status { get; init; }
    }
}
