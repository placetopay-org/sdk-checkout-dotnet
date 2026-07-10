namespace PlaceToPay.Checkout;

/// <summary>
/// Preset PlaceToPay Checkout environments.
/// </summary>
/// <remarks>
/// Each value resolves to a default base URL in <see cref="CheckoutClientOptions"/>.
/// Callers can always override by setting <see cref="CheckoutClientOptions.BaseUrl"/>
/// explicitly. Note that this enum intentionally shadows <c>System.Environment</c>;
/// use <c>PlaceToPay.Checkout.Environment</c> fully qualified if you need both.
/// </remarks>
public enum Environment
{
    /// <summary>Default sandbox URL — https://checkout-test.placetopay.com.</summary>
    Sandbox,

    /// <summary>Default production URL — https://checkout.placetopay.com.</summary>
    Production,
}
