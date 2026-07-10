using System.Text.Json;
using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Models.Transaction;

/// <summary>
/// A single settled (or attempted) transaction inside a session's <c>payment</c> array.
/// </summary>
public sealed record Transaction
{
    [JsonPropertyName("status")] public Status? Status { get; init; }
    [JsonPropertyName("reference")] public string? Reference { get; init; }
    [JsonPropertyName("internalReference")] public long? InternalReference { get; init; }
    [JsonPropertyName("paymentMethod")] public string? PaymentMethod { get; init; }
    [JsonPropertyName("paymentMethodName")] public string? PaymentMethodName { get; init; }
    [JsonPropertyName("issuerName")] public string? IssuerName { get; init; }
    [JsonPropertyName("amount")] public AmountConversion? Amount { get; init; }
    [JsonPropertyName("authorization")] public string? Authorization { get; init; }
    [JsonPropertyName("receipt")] public string? Receipt { get; init; }
    [JsonPropertyName("franchise")] public string? Franchise { get; init; }
    [JsonPropertyName("refunded")] public bool? Refunded { get; init; }
    [JsonPropertyName("processorFields")] public IReadOnlyList<ProcessorField>? ProcessorFields { get; init; }
    [JsonPropertyName("agreement")] public string? Agreement { get; init; }
    [JsonPropertyName("agreementType")] public string? AgreementType { get; init; }
    [JsonPropertyName("dispersion")] public JsonElement? Dispersion { get; init; }
    [JsonPropertyName("discount")] public Discount? Discount { get; init; }
    [JsonPropertyName("subscription")] public SubscriptionInformation? Subscription { get; init; }

    /// <summary>True when <see cref="Status"/> is APPROVED (strict — NOT including APPROVED_PARTIAL or OK).</summary>
    [JsonIgnore]
    public bool IsApproved => Status?.IsApproved ?? false;

    /// <summary>True when <see cref="Status"/> is REJECTED.</summary>
    [JsonIgnore]
    public bool IsRejected => Status?.IsRejected ?? false;

    /// <summary>True when <see cref="Status"/> is any "good" code (OK / APPROVED / APPROVED_PARTIAL).</summary>
    [JsonIgnore]
    public bool IsSuccessful => Status?.IsSuccessful ?? false;
}
