using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Instrument;

/// <summary>Stored payment instrument token issued after a successful `subscribe` flow.</summary>
public sealed record Token
{
    /// <summary>The token string itself. Masked in logs except for the last 4 chars.</summary>
    [JsonPropertyName("token")] public string? TokenValue { get; init; }
    [JsonPropertyName("subtoken")] public string? Subtoken { get; init; }
    [JsonPropertyName("franchise")] public string? Franchise { get; init; }
    [JsonPropertyName("franchiseName")] public string? FranchiseName { get; init; }
    [JsonPropertyName("issuerName")] public string? IssuerName { get; init; }
    [JsonPropertyName("lastDigits")] public string? LastDigits { get; init; }
    [JsonPropertyName("validUntil")] public DateTimeOffset? ValidUntil { get; init; }
    [JsonPropertyName("installments")] public int? Installments { get; init; }
    /// <summary>CVV supplied by the payer at collect time. NEVER logged.</summary>
    [JsonPropertyName("cvv")] public string? Cvv { get; init; }
}
