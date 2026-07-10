using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Common;

/// <summary>
/// Explicit discount entity — mirrors the PHP SDK's <c>Discount</c> class.
/// </summary>
/// <remarks>
/// The gateway may return this object inside an approved transaction's
/// <c>discount</c> slot. The SDK doesn't require callers to construct one for
/// outgoing requests — it's primarily a response shape.
/// </remarks>
public sealed record Discount
{
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("amount")] public decimal? Amount { get; init; }
    [JsonPropertyName("base")] public decimal? Base { get; init; }
    [JsonPropertyName("percent")] public decimal? Percent { get; init; }
}
