using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Internal;
using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Models.Transaction;

public sealed record SubscriptionInformation
{
    [JsonConverter(typeof(EnumMemberJsonConverter<SubscriptionType>))]
    [JsonPropertyName("type")] public SubscriptionType? Type { get; init; }
    [JsonPropertyName("status")] public Status? Status { get; init; }
    [JsonPropertyName("instrument")] public IReadOnlyList<NameValuePair>? Instrument { get; init; }
}

[JsonConverter(typeof(EnumMemberJsonConverter<SubscriptionType>))]
public enum SubscriptionType
{
    [EnumMember(Value = "token")] Token,
    [EnumMember(Value = "recurring")] Recurring,
}
