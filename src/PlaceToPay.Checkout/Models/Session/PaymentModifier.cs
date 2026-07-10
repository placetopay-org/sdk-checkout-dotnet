using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Session;

/// <summary>Modifier applied to a payment — e.g. federal government entity tax rules.</summary>
public sealed record PaymentModifier
{
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("additional")] public IReadOnlyDictionary<string, string>? Additional { get; init; }
}
