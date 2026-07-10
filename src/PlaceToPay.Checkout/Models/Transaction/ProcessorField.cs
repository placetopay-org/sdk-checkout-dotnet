using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Transaction;

/// <summary>
/// Processor metadata returned inside transaction responses.
///
/// The gateway can emit heterogeneous JSON values (string, number, boolean,
/// object), so <see cref="Value"/> is modeled as <see cref="object"/>.
/// </summary>
public sealed record ProcessorField
{
    [JsonPropertyName("keyword")] public string? Keyword { get; init; }
    [JsonPropertyName("value")] public object? Value { get; init; }
    [JsonPropertyName("displayOn")] public string? DisplayOn { get; init; }
}
