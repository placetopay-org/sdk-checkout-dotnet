using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Instrument;
using PlaceToPay.Checkout.Models.Transaction;

namespace PlaceToPay.Checkout.Models.Session;

public sealed record Payment
{
    /// <summary>Merchant's unique reference for this transaction.</summary>
    [JsonPropertyName("reference")] public required string Reference { get; init; }

    [JsonPropertyName("description")] public required string Description { get; init; }

    [JsonPropertyName("amount")] public required Amount Amount { get; init; }

    [JsonPropertyName("allowPartial")] public bool? AllowPartial { get; init; }

    [JsonPropertyName("shipping")] public Person? Shipping { get; init; }

    [JsonPropertyName("items")] public IReadOnlyList<Item>? Items { get; init; }

    [JsonPropertyName("fields")] public IReadOnlyList<NameValuePair>? Fields { get; init; }

    [JsonPropertyName("recurring")] public Recurring? Recurring { get; init; }

    /// <summary>If true, tokenize the instrument after a successful payment.</summary>
    [JsonPropertyName("subscribe")] public bool? Subscribe { get; init; }

    [JsonPropertyName("dispersion")] public IReadOnlyList<DispersionItem>? Dispersion { get; init; }

    [JsonPropertyName("modifiers")] public IReadOnlyList<PaymentModifier>? Modifiers { get; init; }

    [JsonPropertyName("agreement")] public string? Agreement { get; init; }
    [JsonPropertyName("agreementType")] public string? AgreementType { get; init; }

    [JsonPropertyName("discount")] public AmountDetail? Discount { get; init; }

    [JsonPropertyName("instrument")] public Instrument.Instrument? Instrument { get; init; }
}
