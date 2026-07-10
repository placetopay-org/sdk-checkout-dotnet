using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Errors;

/// <summary>
/// Raised on HTTP 5xx — the server is the source of the error. The SDK has already
/// retried per the retry policy by the time this bubbles up.
/// </summary>
public sealed class ServerException : PlaceToPayException
{
    /// <summary>Constructs a <see cref="ServerException"/>.</summary>
    /// <param name="message">Human-readable message.</param>
    /// <param name="statusCode">HTTP status code (e.g. 500, 502, 503).</param>
    /// <param name="status">Status object parsed from the response, if available.</param>
    /// <param name="reason">Short reason code from the API.</param>
    /// <param name="requestId">Request id, if known.</param>
    /// <param name="rawBody">Raw response body for debugging.</param>
    /// <param name="innerException">Underlying exception, if any.</param>
    public ServerException(
        string message,
        int statusCode,
        Status? status = null,
        string? reason = null,
        string? requestId = null,
        string? rawBody = null,
        Exception? innerException = null)
        : base(message, status, reason, requestId, rawBody, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>The HTTP status code (e.g. 500, 502, 503).</summary>
    public int StatusCode { get; }
}
