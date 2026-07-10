using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Authentication;

namespace PlaceToPay.Checkout.Internal;

/// <summary>
/// Internal body used for <c>querySession</c>, <c>cancelSession</c>, and any other
/// endpoints where the wire request is only the <see cref="Authentication.Auth"/>
/// block.
/// </summary>
internal sealed record AuthOnlyBody
{
    [JsonPropertyName("auth")] public required Auth Auth { get; init; }
}
