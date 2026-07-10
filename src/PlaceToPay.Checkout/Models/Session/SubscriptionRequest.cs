using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Models.Session;

public sealed record SubscriptionRequest
{
    [JsonPropertyName("reference")] public required string Reference { get; init; }
    [JsonPropertyName("description")] public required string Description { get; init; }
    [JsonPropertyName("fields")] public IReadOnlyList<NameValuePair>? Fields { get; init; }
}
