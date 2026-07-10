using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Errors;

/// <summary>
/// Raised on HTTP 400 — request did not pass server-side validation.
/// </summary>
public sealed class ValidationException : PlaceToPayException
{
    /// <summary>Constructs a <see cref="ValidationException"/>.</summary>
    /// <param name="message">Human-readable message.</param>
    /// <param name="status">Status object parsed from the response, if available.</param>
    /// <param name="reason">Short reason code from the API.</param>
    /// <param name="requestId">Request id, if known.</param>
    /// <param name="rawBody">Raw response body for debugging.</param>
    /// <param name="fieldErrors">Field-level errors parsed from the response body.</param>
    /// <param name="innerException">Underlying exception, if any.</param>
    public ValidationException(
        string message,
        Status? status = null,
        string? reason = null,
        string? requestId = null,
        string? rawBody = null,
        IReadOnlyList<FieldError>? fieldErrors = null,
        Exception? innerException = null)
        : base(message, status, reason, requestId, rawBody, innerException)
    {
        FieldErrors = fieldErrors ?? Array.Empty<FieldError>();
    }

    /// <summary>Field-level errors extracted from the response body, if present.</summary>
    public IReadOnlyList<FieldError> FieldErrors { get; }

    /// <summary>A single field-level validation error.</summary>
    /// <param name="Field">Path of the offending field (e.g. <c>payment.amount.total</c>).</param>
    /// <param name="Message">Server-provided message describing what's wrong.</param>
    public sealed record FieldError(string Field, string? Message);
}
