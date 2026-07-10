using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Internal;

namespace PlaceToPay.Checkout.Models.Common;

public sealed record Person
{
    [JsonPropertyName("document")] public string? Document { get; init; }
    [JsonConverter(typeof(EnumMemberJsonConverter<DocumentType>))]
    [JsonPropertyName("documentType")] public DocumentType? DocumentType { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("surname")] public string? Surname { get; init; }
    [JsonPropertyName("company")] public string? Company { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("mobile")] public string? Mobile { get; init; }
    [JsonPropertyName("address")] public Address? Address { get; init; }
}
