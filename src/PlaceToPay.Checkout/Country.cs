namespace PlaceToPay.Checkout;

/// <summary>
/// Country preset supplying default locale, currency and (for some countries) a base
/// URL override. Compose with <see cref="Environment"/>: e.g. PRODUCTION + ECUADOR
/// resolves to <c>https://checkout.placetopay.ec</c>.
/// </summary>
public enum Country
{
    /// <summary>Colombia — default locale <c>es_CO</c>, currency <c>COP</c>.</summary>
    Colombia,

    /// <summary>Ecuador — default locale <c>es_EC</c>, currency <c>USD</c>; uses <c>checkout.placetopay.ec</c> for production.</summary>
    Ecuador,

    /// <summary>Panama — default locale <c>es_PA</c>, currency <c>USD</c>.</summary>
    Panama,

    /// <summary>Costa Rica — default locale <c>es_CR</c>, currency <c>CRC</c>.</summary>
    CostaRica,
}
