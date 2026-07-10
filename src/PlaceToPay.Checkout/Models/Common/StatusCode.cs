using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Internal;

namespace PlaceToPay.Checkout.Models.Common;

/// <summary>
/// Transaction / session status. Serialized as the exact uppercase token on the wire
/// (e.g. <c>APPROVED</c>, <c>PENDING</c>). Unknown values deserialize to <see cref="Unknown"/>.
/// </summary>
[JsonConverter(typeof(EnumMemberJsonConverter<StatusCode>))]
public enum StatusCode
{
    [EnumMember(Value = "OK")] Ok,
    [EnumMember(Value = "APPROVED")] Approved,
    [EnumMember(Value = "APPROVED_PARTIAL")] ApprovedPartial,
    [EnumMember(Value = "PARTIAL")] Partial,
    [EnumMember(Value = "PENDING")] Pending,
    [EnumMember(Value = "PENDING_VALIDATION")] PendingValidation,
    [EnumMember(Value = "REJECTED")] Rejected,
    [EnumMember(Value = "REFUNDED")] Refunded,
    [EnumMember(Value = "EXPIRED")] Expired,
    [EnumMember(Value = "FAILED")] Failed,
    [EnumMember(Value = "ERROR")] Error,
    [EnumMember(Value = "UNKNOWN")] Unknown,
}
