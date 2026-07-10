using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PlaceToPay.Checkout.Authentication;

/// <summary>
/// Produces an <see cref="Auth"/> block per the algorithm described in
/// ARCHITECTURE.md §3 — SHA-256 of the concatenation
/// <c>rawNonce || utf8(seed) || utf8(secretKey)</c>, base64-encoded.
/// </summary>
/// <remarks>
/// This type is intentionally public so advanced users (e.g. integration-test
/// harnesses) can pre-sign requests with a fixed clock / fixed nonce. In normal
/// usage the <see cref="Http.HttpTransport"/> signs automatically.
/// </remarks>
public static class AuthSigner
{
    /// <summary>Number of random bytes used for the nonce (128 bits).</summary>
    public const int NonceSize = 16;

    /// <summary>
    /// Build an <see cref="Auth"/> block with a freshly generated cryptographic nonce
    /// and the current UTC time as seed. Optional <paramref name="additional"/> metadata
    /// is transmitted alongside the digest but NOT part of the signing input.
    /// </summary>
    public static Auth Sign(string login, string secretKey, IReadOnlyDictionary<string, string>? additional = null)
        => Sign(login, secretKey, DateTimeOffset.UtcNow, additional);

    /// <summary>
    /// Build an <see cref="Auth"/> block at the caller-supplied timestamp. Intended
    /// for tests where a deterministic seed is required.
    /// </summary>
    public static Auth Sign(string login, string secretKey, DateTimeOffset timestamp, IReadOnlyDictionary<string, string>? additional = null)
        => Sign(login, secretKey, timestamp, GenerateNonce(), additional);

    /// <summary>
    /// Lowest-level overload: sign with the caller-supplied timestamp and raw nonce
    /// bytes. Used by the regression test vector.
    /// </summary>
    /// <param name="login">Merchant login.</param>
    /// <param name="secretKey">Merchant secret (never transmitted; only hashed).</param>
    /// <param name="timestamp">Seed timestamp.</param>
    /// <param name="rawNonce">Raw 16-byte nonce; must come from a cryptographic RNG in production.</param>
    /// <param name="additional">Optional auth metadata sent as the <c>additional</c> field; NOT hashed.</param>
    public static Auth Sign(
        string login,
        string secretKey,
        DateTimeOffset timestamp,
        byte[] rawNonce,
        IReadOnlyDictionary<string, string>? additional = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(login);
        ArgumentException.ThrowIfNullOrEmpty(secretKey);
        ArgumentNullException.ThrowIfNull(rawNonce);
        if (rawNonce.Length != NonceSize)
        {
            throw new ArgumentException(
                $"Raw nonce must be exactly {NonceSize} bytes (was {rawNonce.Length}).",
                nameof(rawNonce));
        }

        var seed = FormatSeed(timestamp);

        var seedBytes = Encoding.UTF8.GetBytes(seed);
        var secretBytes = Encoding.UTF8.GetBytes(secretKey);

        // rawNonce || utf8(seed) || utf8(secretKey)  — additional is NOT hashed.
        var buffer = new byte[rawNonce.Length + seedBytes.Length + secretBytes.Length];
        Buffer.BlockCopy(rawNonce, 0, buffer, 0, rawNonce.Length);
        Buffer.BlockCopy(seedBytes, 0, buffer, rawNonce.Length, seedBytes.Length);
        Buffer.BlockCopy(secretBytes, 0, buffer, rawNonce.Length + seedBytes.Length, secretBytes.Length);

        var digest = SHA256.HashData(buffer);

        return new Auth
        {
            Login = login,
            TranKey = Convert.ToBase64String(digest),
            Nonce = Convert.ToBase64String(rawNonce),
            Seed = seed,
            Additional = additional is { Count: > 0 } ? additional : null,
        };
    }

    /// <summary>
    /// Generate a cryptographically-random 16-byte nonce using
    /// <see cref="RandomNumberGenerator"/> (never <see cref="Random"/>).
    /// </summary>
    public static byte[] GenerateNonce() => RandomNumberGenerator.GetBytes(NonceSize);

    /// <summary>
    /// Format a timestamp as ISO 8601 with numeric offset — e.g.
    /// <c>2026-04-22T15:30:45+00:00</c>. Identical across all three SDKs.
    /// </summary>
    public static string FormatSeed(DateTimeOffset timestamp)
        => timestamp.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
}
