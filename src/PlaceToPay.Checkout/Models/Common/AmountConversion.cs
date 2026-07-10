using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Common;

/// <summary>
/// Amount with currency conversion applied (as reported by the gateway).
/// Appears inside <c>Transaction.amount</c>.
/// </summary>
public sealed record AmountConversion
{
    [JsonPropertyName("from")] public AmountBase? From { get; init; }
    [JsonPropertyName("to")] public AmountBase? To { get; init; }
    [JsonPropertyName("factor")] public decimal? Factor { get; init; }
}
