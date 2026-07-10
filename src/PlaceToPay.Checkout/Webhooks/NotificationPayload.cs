using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Webhooks;

/// <summary>
/// Async notification payload that PlaceToPay POSTs to the merchant's configured
/// URL after a session resolves. Use <see cref="NotificationVerifier"/> to
/// authenticate this payload before trusting it.
/// </summary>
public sealed record NotificationPayload
{
    /// <summary>Numeric session/request identifier the notification refers to.</summary>
    [JsonPropertyName("requestId")] public long RequestId { get; init; }

    /// <summary>Merchant-supplied reference echoed back from the original session.</summary>
    [JsonPropertyName("reference")] public string? Reference { get; init; }

    /// <summary>Lowercase hex SHA-256 (or SHA-1 legacy) signature to verify.</summary>
    [JsonPropertyName("signature")] public required string Signature { get; init; }

    /// <summary>Final status of the session/transaction this notification announces.</summary>
    [JsonPropertyName("status")] public required Status Status { get; init; }
}
