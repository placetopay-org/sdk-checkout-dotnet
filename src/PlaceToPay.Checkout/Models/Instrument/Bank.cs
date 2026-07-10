using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Instrument;

public sealed record Bank
{
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("einterface")] public int? EInterface { get; init; }
}
