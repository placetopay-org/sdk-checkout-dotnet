using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Authentication;

/// <summary>
/// The signed authentication block sent at the top level of every PlaceToPay
/// Checkout request body.
/// </summary>
public sealed record Auth
{
    /// <summary>Merchant login identifier supplied by PlaceToPay.</summary>
    [JsonPropertyName("login")] public required string Login { get; init; }

    /// <summary>Base64-encoded SHA-256 digest of <c>rawNonce || utf8(seed) || utf8(secretKey)</c>.</summary>
    [JsonPropertyName("tranKey")] public required string TranKey { get; init; }

    /// <summary>Base64 of the 16-byte cryptographically-random nonce.</summary>
    [JsonPropertyName("nonce")] public required string Nonce { get; init; }

    /// <summary>ISO 8601 timestamp with offset (e.g. <c>2026-04-22T15:30:45+00:00</c>).</summary>
    [JsonPropertyName("seed")] public required string Seed { get; init; }

    /// <summary>
    /// Optional map transmitted inside the auth block as <c>additional</c>. Mirrors the
    /// PHP SDK's <c>authAdditional</c> configuration. NOT part of the signing digest —
    /// the gateway records it alongside the session as merchant-specific metadata.
    /// </summary>
    [JsonPropertyName("additional")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? Additional { get; init; }
}
