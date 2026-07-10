namespace PlaceToPay.Checkout.Internal;

/// <summary>
/// Static lookup for country-level defaults (locale, currency, optional base URL override).
/// Placeholder URLs per ARCHITECTURE.md §12 until confirmed.
/// </summary>
internal static class CountryPresets
{
    public readonly record struct Preset(string Locale, string Currency, Uri? ProductionBaseUrl, Uri? SandboxBaseUrl);

    public static Preset For(Country country) => country switch
    {
        Country.Colombia => new Preset("es_CO", "COP", null, null),
        Country.Ecuador => new Preset(
            "es_EC",
            "USD",
            new Uri("https://checkout.placetopay.ec"),
            new Uri("https://checkout-test.placetopay.ec")),
        Country.Panama => new Preset("es_PA", "USD", null, null),
        Country.CostaRica => new Preset("es_CR", "CRC", null, null),
        _ => throw new ArgumentOutOfRangeException(nameof(country), country, "Unknown country"),
    };
}
