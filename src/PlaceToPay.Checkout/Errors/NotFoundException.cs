using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Errors;

/// <summary>
/// Raised on HTTP 404 — requested resource (session, transaction, instrument) does not exist.
/// </summary>
public sealed class NotFoundException : PlaceToPayException
{
    /// <summary>Constructs a <see cref="NotFoundException"/>.</summary>
    /// <param name="message">Human-readable message.</param>
    /// <param name="status">Status object parsed from the response, if available.</param>
    /// <param name="reason">Short reason code from the API.</param>
    /// <param name="requestId">Request id, if known.</param>
    /// <param name="rawBody">Raw response body for debugging.</param>
    /// <param name="innerException">Underlying exception, if any.</param>
    public NotFoundException(
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
