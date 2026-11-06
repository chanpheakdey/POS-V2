using PosWebApp.Models.DTOs;

namespace PosWebApp.Infrastructure;

/// <summary>
/// Pure static helper that selects the appropriate <see cref="PriceTierDto"/> for a given quantity.
/// </summary>
public static class PriceTierResolver
{
    /// <summary>
    /// Finds the tier where <c>FromQty &lt;= qty</c> and (<c>ToQty == 0</c> OR <c>qty &lt;= ToQty</c>).
    /// <para>
    /// If no tier matches the quantity exactly, falls back to the tier with the lowest
    /// <c>FromQty</c> value (i.e. the base/introductory tier).
    /// </para>
    /// </summary>
    /// <param name="qty">The quantity to look up. Must be a positive integer.</param>
    /// <param name="tiers">Non-empty list of price tiers for an item.</param>
    /// <returns>The matching <see cref="PriceTierDto"/>, or the lowest-tier fallback. Never null.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tiers"/> is empty.</exception>
    public static PriceTierDto Resolve(int qty, IReadOnlyList<PriceTierDto> tiers)
    {
        if (tiers is null || tiers.Count == 0)
            throw new ArgumentException("Tier list must not be empty.", nameof(tiers));

        // Find a tier where FromQty <= qty AND (ToQty == 0 OR qty <= ToQty)
        var match = tiers.FirstOrDefault(t =>
            t.FromQty <= qty && (t.ToQty == 0 || qty <= t.ToQty));

        if (match is not null)
            return match;

        // Fallback: return the tier with the lowest FromQty
        return tiers.MinBy(t => t.FromQty)!;
    }
}
