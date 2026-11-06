namespace PosWebApp.Infrastructure;

/// <summary>
/// Pure static helper for formatting monetary amounts in USD and KHR.
/// </summary>
public static class CurrencyFormatter
{
    /// <summary>
    /// Formats <paramref name="amount"/> (in USD) as both a USD string and a KHR string.
    /// </summary>
    /// <param name="amount">The amount in US Dollars.</param>
    /// <param name="exchangeRate">KHR units per 1 USD (e.g., 4100).</param>
    /// <returns>
    /// A tuple where:
    /// <list type="bullet">
    ///   <item><description><c>usd</c> — <c>$</c> prefix with exactly 2 decimal places, e.g. <c>$12.50</c></description></item>
    ///   <item><description><c>khr</c> — integer (rounded) with <c>៛</c> suffix, e.g. <c>51,250៛</c></description></item>
    /// </list>
    /// </returns>
    public static (string usd, string khr) Format(double amount, double exchangeRate)
    {
        var usd = $"${amount:F2}";

        var khrValue = (long)Math.Round(amount * exchangeRate, MidpointRounding.AwayFromZero);
        var khr = $"{khrValue:#,0}៛";

        return (usd, khr);
    }
}
