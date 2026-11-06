using System.Globalization;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using PosWebApp.Infrastructure;

namespace PosWebApp.Tests.Unit.Properties;

/// <summary>
/// Property-based tests for <see cref="CurrencyFormatter"/>.
///
/// **Validates: Requirements 5.6, 13.8, 17.7**
/// </summary>
public class FormattingPropertyTests
{
    // ── Arbitrary / generator ─────────────────────────────────────────────────

    /// <summary>
    /// Generates a pair of (non-negative USD amount, positive exchange rate).
    /// Amount: 0.00 to 99,999.99; ExchangeRate: 1.00 to 9,999.00.
    /// </summary>
    public static Arbitrary<(decimal Amount, decimal ExchangeRate)> AmountAndRateArb()
    {
        var gen = Gen.Choose(0, 9_999_999)        // 0 to 99,999.99 USD (in cents)
            .SelectMany(amountCents =>
                Gen.Choose(100, 999900)            // 1.00 to 9,999.00 exchange rate (×100)
                    .Select(rateCents =>
                        ((decimal)amountCents / 100m, (decimal)rateCents / 100m)));

        return gen.ToArbitrary();
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// **Property 10: Currency Formatting Completeness — USD part**
    /// The USD string starts with '$' and has exactly 2 decimal places.
    ///
    /// **Validates: Requirements 5.6, 13.8, 17.7**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(FormattingPropertyTests) }, MaxTest = 1000)]
    public Property Property10_UsdHasDollarSignAndTwoDecimalPlaces(
        (decimal Amount, decimal ExchangeRate) input)
    {
        var (amount, rate) = input;
        var (usd, _) = CurrencyFormatter.Format(amount, rate);

        bool hasDollarSign = usd.StartsWith('$');

        int dotIdx = usd.LastIndexOf('.');
        bool hasTwoDecimalPlaces = dotIdx >= 0 &&
                                   (usd.Length - dotIdx - 1) == 2 &&
                                   usd[(dotIdx + 1)..].All(char.IsDigit);

        return (hasDollarSign && hasTwoDecimalPlaces).Label(
            $"USD '{usd}' should start with '$' and have exactly 2 decimal places " +
            $"(amount={amount}, rate={rate})");
    }

    /// <summary>
    /// **Property 10: Currency Formatting Completeness — KHR part**
    /// The KHR string ends with '៛' and has no decimal point (integer).
    ///
    /// **Validates: Requirements 5.6, 13.8, 17.7**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(FormattingPropertyTests) }, MaxTest = 1000)]
    public Property Property10_KhrHasRielSymbolAndNoDecimalPoint(
        (decimal Amount, decimal ExchangeRate) input)
    {
        var (amount, rate) = input;
        var (_, khr) = CurrencyFormatter.Format(amount, rate);

        bool hasRielSymbol = khr.EndsWith('៛');
        bool isInteger = !khr.Contains('.');

        return (hasRielSymbol && isInteger).Label(
            $"KHR '{khr}' should end with '៛' and contain no decimal point " +
            $"(amount={amount}, rate={rate})");
    }

    /// <summary>
    /// The KHR numeric value equals round(amount × exchangeRate).
    ///
    /// **Validates: Requirements 5.6, 13.8, 17.7**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(FormattingPropertyTests) }, MaxTest = 1000)]
    public Property Property10_KhrValueMatchesRoundedProduct(
        (decimal Amount, decimal ExchangeRate) input)
    {
        var (amount, rate) = input;
        var (_, khr) = CurrencyFormatter.Format(amount, rate);

        string stripped = khr.Replace("៛", "").Replace(",", "").Trim();
        bool parsed = long.TryParse(stripped, out long actual);
        long expected = (long)Math.Round(amount * rate, MidpointRounding.AwayFromZero);

        return (parsed && actual == expected).Label(
            $"KHR should be {expected} but got '{stripped}' " +
            $"(amount={amount}, rate={rate})");
    }

    /// <summary>
    /// The USD string numeric value equals the original amount rounded to 2 d.p.
    ///
    /// **Validates: Requirements 5.6, 13.8, 17.7**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(FormattingPropertyTests) }, MaxTest = 1000)]
    public Property Property10_UsdValuePreservesAmount(
        (decimal Amount, decimal ExchangeRate) input)
    {
        var (amount, rate) = input;
        var (usd, _) = CurrencyFormatter.Format(amount, rate);

        string stripped = usd.TrimStart('$');
        bool parsed = decimal.TryParse(stripped, NumberStyles.Number,
            CultureInfo.InvariantCulture, out decimal parsedAmount);

        bool isClose = parsed && Math.Abs(parsedAmount - Math.Round(amount, 2)) < 0.005m;

        return isClose.Label(
            $"USD '{stripped}' should round-trip to {amount:F2} " +
            $"(amount={amount}, rate={rate})");
    }

    // ── Example-based edge cases ──────────────────────────────────────────────

    [Fact]
    public void ZeroAmount_FormatsCorrectly()
    {
        var (usd, khr) = CurrencyFormatter.Format(0m, 4100m);
        Assert.Equal("$0.00", usd);
        Assert.Equal("0៛", khr);
    }

    [Fact]
    public void TypicalAmount_FormatsCorrectly()
    {
        // 12.50 × 4100 = 51,250
        var (usd, khr) = CurrencyFormatter.Format(12.50m, 4100m);
        Assert.Equal("$12.50", usd);
        Assert.Equal("51,250៛", khr);
    }

    [Fact]
    public void LargeAmount_HasThousandSeparatorInKhr()
    {
        // 100 × 4100 = 410,000
        var (_, khr) = CurrencyFormatter.Format(100.00m, 4100m);
        Assert.Equal("410,000៛", khr);
    }

    [Fact]
    public void SmallAmount_SmallKhrValue()
    {
        // 0.01 × 4100 = 41
        var (usd, khr) = CurrencyFormatter.Format(0.01m, 4100m);
        Assert.Equal("$0.01", usd);
        Assert.Equal("41៛", khr);
    }
}
