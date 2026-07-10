using PlaceToPay.Checkout.Models.Collect;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Instrument;
using PlaceToPay.Checkout.Models.Reverse;
using PlaceToPay.Checkout.Models.Session;

namespace PlaceToPay.Checkout;

/// <summary>
/// Async client for the PlaceToPay Checkout API. Construct via
/// <see cref="CheckoutClient"/> or register with DI using
/// <c>AddPlaceToPayCheckout</c>.
/// </summary>
/// <remarks>
/// All methods issue a single HTTP request, signed on-the-fly with a freshly-generated
/// authentication block. Retries (exponential backoff, honoring <c>Retry-After</c>)
/// are governed by <see cref="CheckoutClientOptions"/>.
/// </remarks>
public interface ICheckoutClient
{
    /// <summary>The effective options (including the resolved base URL) for this client.</summary>
    CheckoutClientOptions Options { get; }

    /// <summary><c>POST /api/session</c> — create a new checkout session.</summary>
    /// <param name="request">Session parameters. <see cref="CreateSessionRequest.Auth"/> is ignored; the SDK signs automatically.</param>
    /// <param name="idempotencyKey">Optional idempotency key, sent as <c>X-Idempotency-Key</c>.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<CreateSessionResponse> CreateSessionAsync(
        CreateSessionRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/session/{requestId}</c> — fetch a session's state.</summary>
    Task<SessionInformation> QuerySessionAsync(long requestId, CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/session/{requestId}/cancel</c> — cancel a pending session.</summary>
    Task<CancelSessionResponse> CancelSessionAsync(long requestId, CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/collect</c> — direct charge on a stored instrument.</summary>
    Task<SessionInformation> CollectAsync(
        CollectRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/reverse</c> — reverse a previously approved transaction.</summary>
    Task<ReverseResponse> ReverseAsync(
        string internalReference,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/instrument/invalidate</c> — revoke a stored token.</summary>
    Task<Status> InvalidateTokenAsync(
        InvalidateTokenRequest request,
        CancellationToken cancellationToken = default);
}
