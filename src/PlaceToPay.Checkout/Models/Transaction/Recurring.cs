using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Internal;

namespace PlaceToPay.Checkout.Models.Transaction;

/// <summary>Recurring payment schedule.</summary>
public sealed record Recurring
{
    [JsonConverter(typeof(EnumMemberJsonConverter<Periodicity>))]
    [JsonPropertyName("periodicity")] public Periodicity? Periodicity { get; init; }
    [JsonPropertyName("interval")] public int? Interval { get; init; }
    [JsonPropertyName("nextPayment")] public DateOnly? NextPayment { get; init; }
    /// <summary>-1 means infinite.</summary>
    [JsonPropertyName("maxPeriods")] public int? MaxPeriods { get; init; }
    [JsonPropertyName("dueDate")] public DateOnly? DueDate { get; init; }
    [JsonPropertyName("notificationUrl")] public string? NotificationUrl { get; init; }
}

[JsonConverter(typeof(EnumMemberJsonConverter<Periodicity>))]
public enum Periodicity
{
    [EnumMember(Value = "D")] Daily,
    [EnumMember(Value = "M")] Monthly,
    [EnumMember(Value = "Y")] Yearly,
}
