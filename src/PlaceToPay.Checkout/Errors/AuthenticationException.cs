using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Errors;

/// <summary>
/// Raised on HTTP 401 — bad login, bad digest, or expired seed.
/// </summary>
public sealed class AuthenticationException : PlaceToPayException
{
    /// <summary>Constructs an <see cref="AuthenticationException"/>.</summary>
    /// <param name="message">Human-readable message.</param>
    /// <param name="status">Status object parsed from the response, if available.</param>
    /// <param name="reason">Short reason code from the API.</param>
    /// <param name="requestId">Request id, if known.</param>
    /// <param name="rawBody">Raw response body for debugging.</param>
    /// <param name="innerException">Underlying exception, if any.</param>
    public AuthenticationException(
        string message,
        Status? status = null,
        string? reason = null,
        string? requestId = null,
        string? rawBody = null,
        Exception? innerException = null)
        : base(message, status, reason, requestId, rawBody, innerException)
    {
    }
}
