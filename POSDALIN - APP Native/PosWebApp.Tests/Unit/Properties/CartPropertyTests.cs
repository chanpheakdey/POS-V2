using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using PosWebApp.Models.DTOs;
using PosWebApp.Models.ViewModels;
using PosWebApp.Services;

namespace PosWebApp.Tests.Unit.Properties;

/// <summary>
/// Property-based tests for cart discount calculations on <see cref="CartItem"/>.
///
/// **Validates: Requirements 30.1, 30.2, 30.3**
/// </summary>
public class CartPropertyTests
{
    // ── Arbitrary / generator ─────────────────────────────────────────────────

    /// <summary>
    /// Generates a pair of (price P, discount D) where:
    /// <list type="bullet">
    ///   <item>P is in the range [0.01, 9999.99] (a positive decimal price)</item>
    ///   <item>D is in the range [0, 100] (a valid percentage discount)</item>
    /// </list>
    /// </summary>
    public static Arbitrary<(decimal Price, decimal Discount)> PriceAndDiscountArb()
    {
        // Price: 1..999999 cents → 0.01..9999.99
        var gen = Gen.Choose(1, 999_999)
            .SelectMany(priceCents =>
                // Discount: 0..10000 → 0.00..100.00 (two decimal places)
                Gen.Choose(0, 10_000)
                    .Select(discountCents =>
                        ((decimal)priceCents / 100m, (decimal)discountCents / 100m)));

        return gen.ToArbitrary();
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// **Property 13: Discount Price Invariant — formula correctness**
    /// For any price P > 0 and discount D where 0 ≤ D ≤ 100,
    /// the discounted price equals P × (1 − D/100).
    ///
    /// Verified via <see cref="CartItem.LineTotal"/> with Quantity = 1
    /// so that LineTotal = UnitPrice × (1 − Discount/100).
    ///
    /// **Validates: Requirements 30.1, 30.2, 30.3**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CartPropertyTests) }, MaxTest = 1000)]
    public Property Property13_DiscountedPriceEqualsFormula(
        (decimal Price, decimal Discount) input)
    {
        var (price, discount) = input;

        var item = new CartItem(
            ItemId: 1,
            ItemCode: "TEST",
            ItemName: "Test Item",
            ImageUrl: "",
            Quantity: 1,
            UnitPrice: price,
            PriceTierId: 1,
            Discount: discount);

        decimal expected = price * (1 - discount / 100m);
        bool formulaCorrect = item.LineTotal == expected;

        return formulaCorrect.Label(
            $"LineTotal should equal P×(1−D/100): " +
            $"expected={expected:F6}, actual={item.LineTotal:F6} " +
            $"(P={price}, D={discount})");
    }

    /// <summary>
    /// **Property 13: Discount Price Invariant — discounted price ≤ original price**
    /// For any price P > 0 and discount D where 0 ≤ D ≤ 100,
    /// the discounted price is always less than or equal to the original price P.
    ///
    /// **Validates: Requirements 30.1, 30.2, 30.3**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CartPropertyTests) }, MaxTest = 1000)]
    public Property Property13_DiscountedPriceNeverExceedsOriginal(
        (decimal Price, decimal Discount) input)
    {
        var (price, discount) = input;

        var item = new CartItem(
            ItemId: 1,
            ItemCode: "TEST",
            ItemName: "Test Item",
            ImageUrl: "",
            Quantity: 1,
            UnitPrice: price,
            PriceTierId: 1,
            Discount: discount);

        bool doesNotExceed = item.LineTotal <= price;

        return doesNotExceed.Label(
            $"Discounted price must be ≤ original: " +
            $"lineTotal={item.LineTotal:F6}, price={price} " +
            $"(P={price}, D={discount})");
    }

    /// <summary>
    /// **Property 13: Discount Price Invariant — zero discount yields original price**
    /// When discount D = 0, the discounted price equals the original price P exactly.
    ///
    /// **Validates: Requirements 30.1, 30.2, 30.3**
    /// </summary>
    [Property(MaxTest = 500)]
    public Property Property13_ZeroDiscountYieldsOriginalPrice(PositiveInt priceCents)
    {
        decimal price = (decimal)priceCents.Get / 100m;

        var item = new CartItem(
            ItemId: 1,
            ItemCode: "TEST",
            ItemName: "Test Item",
            ImageUrl: "",
            Quantity: 1,
            UnitPrice: price,
            PriceTierId: 1,
            Discount: 0m);

        bool isExact = item.LineTotal == price;

        return isExact.Label(
            $"Zero discount must yield original price exactly: " +
            $"lineTotal={item.LineTotal}, price={price}");
    }

    // ── Example-based edge cases ──────────────────────────────────────────────

    [Fact]
    public void ZeroDiscount_LineTotalEqualsUnitPrice()
    {
        var item = new CartItem(1, "A", "Item A", "", 1, 50.00m, 1, Discount: 0m);
        Assert.Equal(50.00m, item.LineTotal);
    }

    [Fact]
    public void FullDiscount_LineTotalIsZero()
    {
        var item = new CartItem(1, "A", "Item A", "", 1, 50.00m, 1, Discount: 100m);
        Assert.Equal(0m, item.LineTotal);
    }

    [Fact]
    public void TenPercentDiscount_ReducesByTenPercent()
    {
        var item = new CartItem(1, "A", "Item A", "", 1, 100.00m, 1, Discount: 10m);
        // 100 * (1 - 10/100) = 90.00
        Assert.Equal(90.00m, item.LineTotal);
    }

    [Fact]
    public void FiftyPercentDiscount_HalvesPrice()
    {
        var item = new CartItem(1, "A", "Item A", "", 1, 80.00m, 1, Discount: 50m);
        // 80 * (1 - 50/100) = 40.00
        Assert.Equal(40.00m, item.LineTotal);
    }

    [Fact]
    public void DiscountWithQuantity_LineTotalAccountsForBoth()
    {
        var item = new CartItem(1, "A", "Item A", "", 3, 20.00m, 1, Discount: 25m);
        // 20 * (1 - 25/100) * 3 = 20 * 0.75 * 3 = 45.00
        Assert.Equal(45.00m, item.LineTotal);
    }
}

/// <summary>
/// Property-based tests for cart quantity accumulation via <see cref="ICartService.AddItemPure"/>.
///
/// **Validates: Requirements 6.1, 6.2**
/// </summary>
public class CartQuantityAccumulationPropertyTests
{
    /// <summary>
    /// **Property 3: Cart Quantity Accumulation**
    ///
    /// For any sequence of positive add-to-cart increments applied to the same item,
    /// the final quantity in the cart SHALL equal the arithmetic sum of all individual
    /// increment amounts.
    ///
    /// Generator: a positive item ID and a non-empty array of positive integer increments.
    /// Steps: start from an empty <see cref="CartState"/>, apply each increment in order
    /// via <see cref="ICartService.AddItemPure"/> (no session I/O), then assert the
    /// final quantity equals the sum of the increments.
    ///
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Property(MaxTest = 1000)]
    public Property Property3_CartQuantityEqualsSum(
        PositiveInt itemId,
        NonEmptyArray<PositiveInt> increments)
    {
        // A single flat-rate tier covering every quantity (ToQty = 0 means no upper bound).
        // Price tier selection is not the concern of this property — we keep it trivial.
        var tiers = new[]
        {
            new PriceTierDto(
                ID:       1,
                ItemID:   itemId.Get,
                FromQty:  1,
                ToQty:    0,
                Price:    10m,
                Currency: "USD")
        };

        // Apply each increment sequentially via the pure helper (no session I/O).
        var cart = CartState.Empty;
        foreach (var inc in increments.Get)
        {
            var request = new AddToCartRequest
            {
                ItemId   = itemId.Get,
                ItemCode = "TST",
                ItemName = "Test Item",
                ImageUrl = string.Empty,
                Quantity = inc.Get,
            };
            cart = ICartService.AddItemPure(cart, request, tiers);
        }

        int expected = increments.Get.Sum(i => i.Get);
        int actual   = cart.Items.Single(i => i.ItemId == itemId.Get).Quantity;

        return (actual == expected).Label(
            $"ItemId={itemId.Get}, " +
            $"Increments=[{string.Join(",", increments.Get.Select(i => i.Get))}], " +
            $"Expected={expected}, Actual={actual}");
    }
}
