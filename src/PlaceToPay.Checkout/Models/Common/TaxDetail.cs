using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Common;

public sealed record TaxDetail
{
    [JsonPropertyName("kind")] public required string Kind { get; init; }
    [JsonPropertyName("amount")] public required decimal Amount { get; init; }
    [JsonPropertyName("base")] public required decimal Base { get; init; }
}
