using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Authentication;

namespace PlaceToPay.Checkout.Models.Instrument;

/// <summary>
/// Request body for <c>POST /api/instrument/invalidate</c>. The <see cref="Auth"/>
/// block is injected automatically by <see cref="CheckoutClient"/>.
/// </summary>
public sealed record InvalidateTokenRequest
{
    [JsonPropertyName("auth")] public Auth? Auth { get; init; }
    [JsonPropertyName("locale")] public string? Locale { get; init; }
    [JsonPropertyName("instrument")] public required Instrument Instrument { get; init; }
}
