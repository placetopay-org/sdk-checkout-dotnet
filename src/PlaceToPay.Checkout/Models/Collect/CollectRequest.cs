using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Authentication;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Session;

namespace PlaceToPay.Checkout.Models.Collect;

/// <summary>
/// Request body for <c>POST /api/collect</c> (direct charge against a stored
/// instrument). <see cref="Auth"/> is injected automatically.
/// </summary>
public sealed record CollectRequest
{
    [JsonPropertyName("auth")] public Auth? Auth { get; init; }
    [JsonPropertyName("locale")] public string? Locale { get; init; }
    [JsonPropertyName("payer")] public required Person Payer { get; init; }
    [JsonPropertyName("buyer")] public Person? Buyer { get; init; }
    [JsonPropertyName("payment")] public required Payment Payment { get; init; }
    [JsonPropertyName("instrument")] public required Instrument.Instrument Instrument { get; init; }
    [JsonPropertyName("ipAddress")] public string? IpAddress { get; init; }
    [JsonPropertyName("userAgent")] public string? UserAgent { get; init; }
    [JsonPropertyName("fields")] public IReadOnlyList<NameValuePair>? Fields { get; init; }

    /// <summary>
    /// Optional processor override. Mirrors the PHP SDK's <c>provider</c> field —
    /// pins the collect call to a specific payment processor when the gateway has
    /// more than one route available.
    /// </summary>
    [JsonPropertyName("provider")] public string? Provider { get; init; }
}
