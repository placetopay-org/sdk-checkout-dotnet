using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Authentication;
using PlaceToPay.Checkout.Internal;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Transaction;

namespace PlaceToPay.Checkout.Models.Session;

/// <summary>
/// Request body for <c>POST /api/session</c>. At least one of
/// <see cref="Payment"/>, <see cref="Subscription"/>, or <see cref="Autopay"/>
/// must be populated. The <see cref="Auth"/> block is set automatically by
/// the SDK.
/// </summary>
public sealed record CreateSessionRequest
{
    [JsonPropertyName("auth")] public Auth? Auth { get; init; }

    [JsonPropertyName("locale")] public string? Locale { get; init; }

    [JsonPropertyName("buyer")] public Person? Buyer { get; init; }

    [JsonPropertyName("payer")] public Person? Payer { get; init; }

    [JsonPropertyName("payment")] public Payment? Payment { get; init; }

    [JsonPropertyName("subscription")] public SubscriptionRequest? Subscription { get; init; }

    [JsonPropertyName("returnUrl")] public required string ReturnUrl { get; init; }

    [JsonPropertyName("cancelUrl")] public string? CancelUrl { get; init; }

    [JsonPropertyName("ipAddress")] public required string IpAddress { get; init; }

    [JsonPropertyName("userAgent")] public required string UserAgent { get; init; }

    /// <summary>Session expiration; must be at least 5 minutes from creation.</summary>
    [JsonPropertyName("expiration")] public DateTimeOffset? Expiration { get; init; }

    /// <summary>Comma-separated method codes, e.g. <c>"visa,master,pse"</c>.</summary>
    [JsonPropertyName("paymentMethod")] public string? PaymentMethod { get; init; }

    [JsonPropertyName("skipResult")] public bool? SkipResult { get; init; }

    [JsonPropertyName("noBuyerFill")] public bool? NoBuyerFill { get; init; }

    [JsonPropertyName("captureAddress")] public bool? CaptureAddress { get; init; }

    [JsonPropertyName("fields")] public IReadOnlyList<NameValuePair>? Fields { get; init; }

    [JsonConverter(typeof(EnumMemberJsonConverter<SessionType>))]
    [JsonPropertyName("type")] public SessionType? Type { get; init; }

    [JsonPropertyName("autopay")] public AutopayRequest? Autopay { get; init; }

    /// <summary>
    /// Free-form merchant metadata. Canonical keys the PlaceToPay API
    /// recognizes (v2.26.8+): <c>openingDate</c> (ISO-8601 timestamp for
    /// when the customer opened the session/cart) and <c>beneficiaryId</c>
    /// (identifier of the final beneficiary for regulated verticals).
    /// Extra keys are accepted and forwarded as-is.
    /// </summary>
    [JsonPropertyName("metadata")] public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// API v2.27.1 — optional hint about who initiated the payment. Common
    /// values are <c>CIT</c> (cardholder-initiated) and <c>MIT</c>
    /// (merchant-initiated). The gateway validates the accepted vocabulary.
    /// </summary>
    [JsonPropertyName("initiatorIndicator")] public string? InitiatorIndicator { get; init; }
}

[JsonConverter(typeof(EnumMemberJsonConverter<SessionType>))]
public enum SessionType
{
    [EnumMember(Value = "checkin")] Checkin,
    [EnumMember(Value = "autopay")] Autopay,
}
