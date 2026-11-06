namespace PosWebApp.Infrastructure.Options;

/// <summary>
/// Strongly-typed binding for the <c>Payment</c> section in appsettings.json.
/// Provides the KHR exchange rate used by <see cref="PosWebApp.Infrastructure.CurrencyFormatter"/>.
/// </summary>
public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    /// <summary>
    /// KHR units per 1 USD. Defaults to 4100.
    /// </summary>
    public decimal KhrExchangeRate { get; set; } = 4100m;
}
