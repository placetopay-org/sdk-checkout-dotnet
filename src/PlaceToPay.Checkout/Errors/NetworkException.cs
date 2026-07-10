namespace PlaceToPay.Checkout.Errors;

/// <summary>
/// Raised when no HTTP response was obtained — socket/TLS/DNS errors, timeouts, or
/// the operation being cancelled after exhausting retries.
/// </summary>
public sealed class NetworkException : PlaceToPayException
{
    /// <summary>Constructs a <see cref="NetworkException"/>.</summary>
    /// <param name="message">Human-readable message.</param>
    /// <param name="innerException">Underlying transport exception (socket, TLS, DNS, etc.).</param>
    public NetworkException(string message, Exception? innerException = null)
        : base(message, status: null, reason: null, requestId: null, rawBody: null, innerException)
    {
    }
}
