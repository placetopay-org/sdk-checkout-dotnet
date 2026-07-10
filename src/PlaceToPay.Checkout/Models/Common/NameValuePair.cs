using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Common;

/// <summary>
/// Key-value pair used by <c>fields</c>, <c>processorFields</c>, and similar
/// free-form maps in the Checkout API.
/// </summary>
public sealed record NameValuePair
{
    [JsonPropertyName("keyword")] public required string Keyword { get; init; }
    [JsonPropertyName("value")] public required string Value { get; init; }
    [JsonPropertyName("displayOn")] public string? DisplayOn { get; init; }
}
