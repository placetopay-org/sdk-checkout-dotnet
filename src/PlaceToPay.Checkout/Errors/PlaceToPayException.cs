using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Errors;

/// <summary>
/// Base class for every error raised by the PlaceToPay Checkout SDK. Never swallowed;
/// every failure path surfaces as a <see cref="PlaceToPayException"/> (or subclass).
/// </summary>
public class PlaceToPayException : Exception
{
    /// <summary>
    /// Primary constructor used by the HTTP transport when it has parsed a JSON body.
    /// </summary>
    public PlaceToPayException(
        string message,
        Status? status = null,
        string? reason = null,
        string? requestId = null,
        string? rawBody = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Status = status;
        Reason = reason ?? status?.Reason;
        RequestId = requestId;
        RawBody = rawBody;
    }

    /// <summary>The API <c>Status</c> object from the response, if the body was JSON.</summary>
    public Status? Status { get; }

    /// <summary>Short reason code (often the same as <c>Status.Reason</c>).</summary>
    public string? Reason { get; }

    /// <summary>Session request id, if one can be inferred from the request or response.</summary>
    public string? RequestId { get; }

    /// <summary>
    /// Raw response body (for debugging). Do NOT parse this programmatically — prefer
    /// <see cref="Status"/> and the typed subclass properties.
    /// </summary>
    public string? RawBody { get; }
}
