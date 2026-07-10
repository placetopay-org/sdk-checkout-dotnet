using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Instrument;

/// <summary>
/// A payment instrument. Exactly one of <see cref="Token"/>, <see cref="Card"/>,
/// <see cref="Bank"/>, or <see cref="Credit"/> should be supplied, depending on
/// the flow.
/// </summary>
public sealed record Instrument
{
    [JsonPropertyName("token")] public Token? Token { get; init; }
    [JsonPropertyName("card")] public Card? Card { get; init; }
    [JsonPropertyName("bank")] public Bank? Bank { get; init; }
    [JsonPropertyName("credit")] public Credit? Credit { get; init; }
    /// <summary>PSE / bank login PIN. NEVER logged.</summary>
    [JsonPropertyName("pin")] public string? Pin { get; init; }
    /// <summary>PSE / bank login password. NEVER logged.</summary>
    [JsonPropertyName("password")] public string? Password { get; init; }
}
