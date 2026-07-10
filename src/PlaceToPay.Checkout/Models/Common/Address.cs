using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Common;

public sealed record Address
{
    [JsonPropertyName("street")] public string? Street { get; init; }
    [JsonPropertyName("city")] public string? City { get; init; }
    [JsonPropertyName("state")] public string? State { get; init; }
    [JsonPropertyName("postalCode")] public string? PostalCode { get; init; }
    [JsonPropertyName("country")] public string? Country { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
}
