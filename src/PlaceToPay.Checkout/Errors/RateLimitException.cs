using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Errors;

/// <summary>
/// Raised on HTTP 429 — the caller exceeded the API rate limit. Respect
/// <see cref="RetryAfter"/> before retrying.
/// </summary>
public sealed class RateLimitException : PlaceToPayException
{
    /// <summary>Constructs a <see cref="RateLimitException"/>.</summary>
    /// <param name="message">Human-readable message.</param>
    /// <param name="retryAfter">Parsed <c>Retry-After</c> header from the response.</param>
    /// <param name="status">Status object parsed from the response, if available.</param>
    /// <param name="reason">Short reason code from the API.</param>
    /// <param name="requestId">Request id, if known.</param>
    /// <param name="rawBody">Raw response body for debugging.</param>
    /// <param name="innerException">Underlying exception, if any.</param>
    public RateLimitException(
        string message,
        TimeSpan? retryAfter = null,
        Status? status = null,
        string? reason = null,
        string? requestId = null,
        string? rawBody = null,
        Exception? innerException = null)
        : base(message, status, reason, requestId, rawBody, innerException)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// The <c>Retry-After</c> value from the response headers, parsed from either a
    /// numeric "seconds" representation or an HTTP-date.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
