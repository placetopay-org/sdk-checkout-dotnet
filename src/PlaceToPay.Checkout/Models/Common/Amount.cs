using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Common;

/// <summary>
/// Full amount with taxes and breakdown.
/// </summary>
public sealed record Amount : AmountBase
{
    [JsonPropertyName("taxes")] public IReadOnlyList<TaxDetail>? Taxes { get; init; }
    [JsonPropertyName("details")] public IReadOnlyList<AmountDetail>? Details { get; init; }
}
