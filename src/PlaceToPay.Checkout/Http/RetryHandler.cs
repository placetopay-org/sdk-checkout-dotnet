using System.Globalization;
using System.Net;
using System.Security.Cryptography;

namespace PlaceToPay.Checkout.Http;

/// <summary>
/// Retry policy helpers. Separated from <see cref="HttpTransport"/> so the math
/// (backoff, jitter, Retry-After parsing) can be unit tested in isolation.
/// </summary>
internal static class RetryHandler
{
    /// <summary>Should <paramref name="statusCode"/> trigger a retry per the SDK policy?</summary>
    public static bool IsRetriableStatus(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code == 429 || code >= 500;
    }

    /// <summary>Should the given exception (thrown before a response arrived) trigger a retry?</summary>
    public static bool IsRetriableException(Exception ex) => ex is HttpRequestException or TaskCanceledException or IOException;

    /// <summary>
    /// Compute the delay for attempt number <paramref name="attempt"/> (1-based: the first
    /// retry is attempt=1). Uses exponential backoff with jitter in the range [0.5, 1.0].
    /// </summary>
    public static TimeSpan ComputeBackoff(int attempt, TimeSpan initialDelay, TimeSpan maxDelay)
    {
        if (attempt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt));
        }

        // initialDelay * 2^(attempt-1), capped at maxDelay, * jitter in [0.5, 1.0].
        var baseMillis = initialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var cappedMillis = Math.Min(baseMillis, maxDelay.TotalMilliseconds);
        var jitter = 0.5 + (NextDouble() * 0.5);
        return TimeSpan.FromMilliseconds(cappedMillis * jitter);
    }

    /// <summary>
    /// Parse the <c>Retry-After</c> response header value, which may be either
    /// an integer number of seconds or an HTTP-date per RFC 9110 §10.2.3.
    /// </summary>
    public static TimeSpan? ParseRetryAfter(string? value, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        if (DateTimeOffset.TryParseExact(
                value,
                ["r", "R", "ddd, dd MMM yyyy HH:mm:ss 'GMT'"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var when))
        {
            var delta = when - clock.UtcNow;
            return delta <= TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        return null;
    }

    // Fill helper — cryptographic rng is overkill for jitter but avoids
    // RandomNumberGenerator vs System.Random debate in the project.
    private static double NextDouble()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        // Drop two high bits so we're non-negative, then scale to [0, 1).
        var longValue = BitConverter.ToInt64(bytes) & 0x3FFFFFFFFFFFFFFF;
        return (double)longValue / 0x4000000000000000L;
    }
}
