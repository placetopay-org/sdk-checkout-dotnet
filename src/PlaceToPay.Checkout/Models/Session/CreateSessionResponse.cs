using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Models.Session;

public sealed record CreateSessionResponse
{
    [JsonPropertyName("status")] public required Status Status { get; init; }
    [JsonPropertyName("requestId")] public long? RequestId { get; init; }
    /// <summary>Where to redirect the end user to complete the payment.</summary>
    [JsonPropertyName("processUrl")] public string? ProcessUrl { get; init; }
}
