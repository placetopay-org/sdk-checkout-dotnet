using System.Security.Cryptography;
using System.Text;

namespace PlaceToPay.Checkout.Webhooks;

/// <summary>
/// Verifies incoming webhook notifications from PlaceToPay.
/// </summary>
/// <remarks>
/// <para>
/// The server builds the signature as:
/// </para>
/// <code>
/// base      = requestId || status.status || status.date || secretKey
/// signature = hex( HASH( base ) )
/// </code>
/// <para>
/// Prefer <see cref="VerifySha256"/>. Use <see cref="VerifySha1Legacy"/> only to
/// interoperate with servers that still emit SHA-1 digests (the behavior of the
/// legacy <c>placetopay/java-placetopay</c> SDK). The public notifications
/// documentation page is currently gated (HTTP 403), so verification can fail
/// until the exact server behavior is confirmed — in that case fall back to the
/// legacy path.
/// </para>
/// </remarks>
public static class NotificationVerifier
{
    /// <summary>Verify the signature using SHA-256 hex.</summary>
    public static bool VerifySha256(NotificationPayload payload, string secretKey)
        => Verify(payload, secretKey, SHA256.HashData);

    /// <summary>Verify the signature using SHA-1 hex (legacy; will be removed in v2).</summary>
    /// <remarks>
    /// <para>
    /// <strong>Prefer <see cref="VerifySha256"/></strong>. SHA-1 is cryptographically
    /// deprecated and this path is retained only for interop with deployments still
    /// on the PHP SDK's default algorithm (<c>$algorithm = 'sha1'</c>).
    /// </para>
    /// <para>
    /// Scheduled for removal in SDK v2.0.0. Open a ticket with PlaceToPay to
    /// enable SHA-256 webhook signing on your merchant account.
    /// </para>
    /// </remarks>
    [Obsolete("SHA-1 is deprecated. Use VerifySha256; this method will be removed in v2.", error: false)]
    public static bool VerifySha1Legacy(NotificationPayload payload, string secretKey)
        => Verify(payload, secretKey, SHA1.HashData);

    private static bool Verify(NotificationPayload payload, string secretKey, Func<byte[], byte[]> hash)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrEmpty(secretKey);
        if (string.IsNullOrWhiteSpace(payload.Signature))
        {
            return false;
        }

        var statusCode = payload.Status?.Code is { } code ? code.ToString().ToUpperInvariant() : string.Empty;
        var date = payload.Status?.Date?.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        var baseString = $"{payload.RequestId}{statusCode}{date}{secretKey}";

        var digest = hash(Encoding.UTF8.GetBytes(baseString));
        var expected = Convert.ToHexString(digest).ToLowerInvariant();

        // Constant-time comparison
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(payload.Signature.Trim().ToLowerInvariant()));
    }
}
