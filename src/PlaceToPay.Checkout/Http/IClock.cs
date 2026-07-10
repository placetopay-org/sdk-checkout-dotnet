namespace PlaceToPay.Checkout.Http;

/// <summary>
/// Internal clock abstraction so tests can fix time for seed generation and
/// retry-after parsing.
/// </summary>
internal interface IClock
{
    DateTimeOffset UtcNow { get; }
}

internal sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
