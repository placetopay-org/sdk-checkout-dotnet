using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Models.Instrument;

public sealed record Credit
{
    /// <summary>Credit code. Redacted in logs.</summary>
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("groupCode")] public string? GroupCode { get; init; }
    [JsonPropertyName("installment")] public int? Installment { get; init; }
}
