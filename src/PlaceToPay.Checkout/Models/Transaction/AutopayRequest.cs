using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Models.Transaction;

/// <summary>Required when <see cref="Session.CreateSessionRequest.Type"/> is
/// <see cref="Session.SessionType.Autopay"/>.</summary>
public sealed record AutopayRequest
{
    [JsonPropertyName("periodicity")] public Periodicity? Periodicity { get; init; }
    [JsonPropertyName("interval")] public int? Interval { get; init; }
    [JsonPropertyName("nextPayment")] public DateOnly? NextPayment { get; init; }
    [JsonPropertyName("maxPeriods")] public int? MaxPeriods { get; init; }
    [JsonPropertyName("amount")] public Amount? Amount { get; init; }
    [JsonPropertyName("dueDate")] public DateOnly? DueDate { get; init; }
    [JsonPropertyName("notificationUrl")] public string? NotificationUrl { get; init; }
}
