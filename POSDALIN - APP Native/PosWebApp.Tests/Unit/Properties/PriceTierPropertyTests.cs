using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using PosWebApp.Infrastructure;
using PosWebApp.Models.DTOs;

namespace PosWebApp.Tests.Unit.Properties;

/// <summary>
/// Property-based tests for <see cref="PriceTierResolver"/>.
///
/// **Validates: Requirements 6.6, 6.10**
/// </summary>
public class PriceTierPropertyTests
{
    // ── Arbitrary / generator ─────────────────────────────────────────────────

    /// <summary>
    /// Generates a pair of (quantity, non-empty tier list) where tiers form
    /// sequential, non-overlapping bands. The last band is always open-ended (ToQty=0).
    /// </summary>
    public static Arbitrary<(int Qty, IReadOnlyList<PriceTierDto> Tiers)> QtyAndTiersArb()
    {
        // bandCount: 1..5 bands
        var gen = Gen.Choose(1, 5).SelectMany(bandCount =>
            // each band size: 1..20 units
            Gen.Choose(1, 20).ListOf(bandCount).SelectMany(bandSizes =>
                // price for each band: 1.00..100.00
                Gen.Choose(100, 10000).Select(p => (decimal)p / 100m).ListOf(bandCount)
                    .SelectMany(prices =>
                    {
                        var tiers = BuildTiers(bandSizes, prices);
                        int maxQty = tiers[^1].FromQty + 50;
                        return Gen.Choose(1, maxQty)
                            .Select(qty => (qty, (IReadOnlyList<PriceTierDto>)tiers));
                    })));

        return gen.ToArbitrary();
    }

    private static List<PriceTierDto> BuildTiers(
        System.Collections.Generic.IList<int> bandSizes,
        System.Collections.Generic.IList<decimal> prices)
    {
        var result = new List<PriceTierDto>();
        int from = 1;
        for (int i = 0; i < bandSizes.Count; i++)
        {
            bool isLast = i == bandSizes.Count - 1;
            int to = isLast ? 0 : from + bandSizes[i] - 1;
            result.Add(new PriceTierDto(i + 1, 1, from, to, prices[i], "USD"));
            if (!isLast)
                from += bandSizes[i];
        }
        return result;
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// **Property 4: Price Tier Resolution**
    /// For any positive integer quantity and any non-empty tier list, the resolved tier
    /// satisfies <c>FromQty &lt;= qty</c> and (<c>ToQty == 0</c> OR <c>qty &lt;= ToQty</c>),
    /// OR it is the lowest-qty fallback tier when no tier matches.
    ///
    /// **Validates: Requirements 6.6, 6.10**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(PriceTierPropertyTests) }, MaxTest = 1000)]
    public Property Property4_ResolvedTierMatchesQtyOrIsFallback(
        (int Qty, IReadOnlyList<PriceTierDto> Tiers) input)
    {
        var (qty, tiers) = input;
        var resolved = PriceTierResolver.Resolve(qty, tiers);

        bool anyMatches = tiers.Any(t =>
            t.FromQty <= qty && (t.ToQty == 0 || qty <= t.ToQty));

        if (anyMatches)
        {
            bool matchCondition = resolved.FromQty <= qty &&
                                  (resolved.ToQty == 0 || qty <= resolved.ToQty);
            return matchCondition.Label(
                $"Matched tier must satisfy FromQty<=qty and (ToQty==0 or qty<=ToQty). " +
                $"qty={qty}, resolved.FromQty={resolved.FromQty}, resolved.ToQty={resolved.ToQty}");
        }
        else
        {
            int lowestFrom = tiers.Min(t => t.FromQty);
            bool fallbackCondition = resolved.FromQty == lowestFrom;
            return fallbackCondition.Label(
                $"No tier matched qty={qty}; fallback must be lowest-FromQty tier " +
                $"(expected {lowestFrom}, got {resolved.FromQty})");
        }
    }

    /// <summary>
    /// For a single open-ended tier (ToQty=0), Resolve always returns that tier.
    ///
    /// **Validates: Requirements 6.6, 6.10**
    /// </summary>
    [Property(MaxTest = 500)]
    public Property Property4_SingleTierAlwaysReturnsItself(PositiveInt qty)
    {
        var tier = new PriceTierDto(1, 1, 1, 0, 9.99m, "USD");
        var tiers = new List<PriceTierDto> { tier };

        var resolved = PriceTierResolver.Resolve(qty.Get, tiers);
        return (resolved == tier).Label($"Single open-ended tier should always be returned (qty={qty.Get})");
    }

    /// <summary>
    /// Resolve never returns null.
    ///
    /// **Validates: Requirements 6.6, 6.10**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(PriceTierPropertyTests) }, MaxTest = 500)]
    public Property Property4_ResolveNeverReturnsNull(
        (int Qty, IReadOnlyList<PriceTierDto> Tiers) input)
    {
        var resolved = PriceTierResolver.Resolve(input.Qty, input.Tiers);
        return (resolved != null).Label($"Resolve must never return null (qty={input.Qty})");
    }

    // ── Example-based edge cases ──────────────────────────────────────────────

    [Fact]
    public void EmptyTierList_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            PriceTierResolver.Resolve(1, new List<PriceTierDto>()));
        Assert.Equal("tiers", ex.ParamName);
    }

    [Fact]
    public void Qty_BelowFirstTier_ReturnsFallback()
    {
        // Bands start at qty=5; qty=2 doesn't match anything, fallback = lowest FromQty=5
        var tiers = new List<PriceTierDto>
        {
            new PriceTierDto(1, 1, 5,  10, 2.00m, "USD"),
            new PriceTierDto(2, 1, 11, 0,  1.50m, "USD"),
        };
        var resolved = PriceTierResolver.Resolve(2, tiers);
        Assert.Equal(5, resolved.FromQty);
    }

    [Fact]
    public void Qty_InOpenEndedTier_ReturnsCorrectTier()
    {
        var tiers = new List<PriceTierDto>
        {
            new PriceTierDto(1, 1, 1,  9,  5.00m, "USD"),
            new PriceTierDto(2, 1, 10, 0,  3.50m, "USD"),
        };
        var resolved = PriceTierResolver.Resolve(100, tiers);
        Assert.Equal(10, resolved.FromQty);
        Assert.Equal(0, resolved.ToQty);
        Assert.Equal(3.50m, resolved.Price);
    }
}
