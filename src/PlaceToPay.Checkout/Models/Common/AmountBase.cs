using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Common;

/// <summary>
/// Bare currency+total pair. Used by <see cref="AmountConversion"/> and
/// dispersion items. For full amounts with taxes/details use <see cref="Amount"/>.
/// </summary>
public record AmountBase
{
    [JsonPropertyName("currency")] public required string Currency { get; init; }
    [JsonPropertyName("total")] public required decimal Total { get; init; }
}
