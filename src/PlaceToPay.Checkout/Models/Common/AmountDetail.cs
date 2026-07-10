using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Common;

public sealed record AmountDetail
{
    /// <summary>
    /// Common values: <c>discount</c>, <c>shipping</c>, <c>tip</c>,
    /// <c>insurance</c>, <c>subtotal</c>.
    /// </summary>
    [JsonPropertyName("kind")] public required string Kind { get; init; }
    [JsonPropertyName("amount")] public required decimal Amount { get; init; }
}
