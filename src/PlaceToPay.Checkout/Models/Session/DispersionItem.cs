using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Models.Session;

/// <summary>
/// Portion of a payment's total amount directed to a sub-merchant account.
/// </summary>
public sealed record DispersionItem
{
    [JsonPropertyName("agreement")] public required string Agreement { get; init; }
    [JsonPropertyName("agreementType")] public required string AgreementType { get; init; }
    [JsonPropertyName("amount")] public required AmountBase Amount { get; init; }
}
