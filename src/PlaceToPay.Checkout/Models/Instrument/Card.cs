using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Internal;

namespace PlaceToPay.Checkout.Models.Instrument;

/// <summary>Card instrument (PAN, expiry, CVV).</summary>
public sealed record Card
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    /// <summary>Primary Account Number. Masked in responses; redacted in logs.</summary>
    [JsonPropertyName("number")] public string? Number { get; init; }
    /// <summary>Card Verification Value. NEVER logged — redacted at the transport layer.</summary>
    [JsonPropertyName("cvv")] public string? Cvv { get; init; }
    [JsonPropertyName("expirationMonth")] public string? ExpirationMonth { get; init; }
    [JsonPropertyName("expirationYear")] public string? ExpirationYear { get; init; }
    [JsonPropertyName("installments")] public int? Installments { get; init; }
    [JsonConverter(typeof(EnumMemberJsonConverter<CardKind>))]
    [JsonPropertyName("kind")] public CardKind? Kind { get; init; }
}

/// <summary>Kind of card for banking routing.</summary>
[JsonConverter(typeof(EnumMemberJsonConverter<CardKind>))]
public enum CardKind
{
    /// <summary>Credit.</summary>
    [EnumMember(Value = "C")] Credit,
    /// <summary>Debit / savings.</summary>
    [EnumMember(Value = "A")] DebitSavings,
    /// <summary>Debit / checking.</summary>
    [EnumMember(Value = "R")] DebitChecking,
}
