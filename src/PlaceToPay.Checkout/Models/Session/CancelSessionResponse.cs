using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Models.Session;

public sealed record CancelSessionResponse
{
    [JsonPropertyName("status")] public required Status Status { get; init; }
    [JsonPropertyName("session")] public SessionInformation? Session { get; init; }
}
