using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Internal;

namespace PlaceToPay.Checkout.Models.Common;

/// <summary>
/// API status block returned inside every response and most error bodies.
/// </summary>
public sealed record Status
{
    [JsonConverter(typeof(EnumMemberJsonConverter<StatusCode>))]
    [JsonPropertyName("status")]
    public StatusCode? Code { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("date")]
    public DateTimeOffset? Date { get; init; }

    /// <summary>
    /// Convenience accessor: true when the underlying <see cref="Code"/> is
    /// <see cref="StatusCode.Ok"/>, <see cref="StatusCode.Approved"/>, or
    /// <see cref="StatusCode.ApprovedPartial"/>.
    /// </summary>
    /// <remarks>
    /// Explicitly NOT the inverted logic from the legacy C# SDK's
    /// <c>Transaction.IsApproved()</c>. This is the corrected semantic.
    /// </remarks>
    [JsonIgnore]
    public bool IsSuccessful => Code is StatusCode.Ok or StatusCode.Approved or StatusCode.ApprovedPartial;

    /// <summary>True when <see cref="Code"/> is exactly <see cref="StatusCode.Approved"/>. Matches PHP's <c>isApproved()</c>.</summary>
    [JsonIgnore]
    public bool IsApproved => Code == StatusCode.Approved;

    /// <summary>True when <see cref="Code"/> is exactly <see cref="StatusCode.Rejected"/>. Matches PHP's <c>isRejected()</c>.</summary>
    [JsonIgnore]
    public bool IsRejected => Code == StatusCode.Rejected;

    /// <summary>True when <see cref="Code"/> is exactly <see cref="StatusCode.Error"/>. Matches PHP's <c>isError()</c>.</summary>
    [JsonIgnore]
    public bool IsError => Code == StatusCode.Error;
}
