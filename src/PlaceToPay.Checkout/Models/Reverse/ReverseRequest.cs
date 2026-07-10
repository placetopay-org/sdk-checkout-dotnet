using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Authentication;

namespace PlaceToPay.Checkout.Models.Reverse;

/// <summary>Body for <c>POST /api/reverse</c>.</summary>
public sealed record ReverseRequest
{
    [JsonPropertyName("auth")] public Auth? Auth { get; init; }
    [JsonPropertyName("internalReference")] public required string InternalReference { get; init; }
}
