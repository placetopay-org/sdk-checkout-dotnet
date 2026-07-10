using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Common;

public sealed record Item
{
    [JsonPropertyName("sku")] public string? Sku { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("category")] public string? Category { get; init; }
    [JsonPropertyName("qty")] public string? Qty { get; init; }
    [JsonPropertyName("price")] public decimal? Price { get; init; }
    [JsonPropertyName("tax")] public decimal? Tax { get; init; }
}
