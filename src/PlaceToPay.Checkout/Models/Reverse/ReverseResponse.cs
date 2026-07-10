using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Models.Reverse;

public sealed record ReverseResponse
{
    [JsonPropertyName("status")] public required Status Status { get; init; }
    [JsonPropertyName("payment")] public Transaction.Transaction? Payment { get; init; }
}
