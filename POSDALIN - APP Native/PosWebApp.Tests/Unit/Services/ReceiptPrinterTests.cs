using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Models.DTOs;
using PosWebApp.Services;

namespace PosWebApp.Tests.Unit.Services;

/// <summary>
/// Property-based tests for <see cref="ReceiptPrinterService"/> receipt completeness.
///
/// Tests target <see cref="ReceiptPrinterService.GenerateTextReceipt"/> — the
/// synchronous receipt method that contains all required fields and does not
/// require a live Razor view engine.
///
/// <see cref="ReceiptPrinterService.GenerateHtmlReceiptAsync"/> requires a running
/// Razor engine and is covered by integration tests instead.
///
/// **Validates: Requirements 26.1, 26.2, 26.3**
/// </summary>
public class ReceiptPrinterTests
{
    // ── Service factory ───────────────────────────────────────────────────────

    private static ReceiptPrinterService BuildService(decimal khrRate = 4100m)
    {
        var receiptOpts = Options.Create(new ReceiptOptions
        {
            BusinessName = "Test Shop",
            Address      = "1 Main St",
            Phone        = "012 345 678",
        });

        var paymentOpts = Options.Create(new PaymentOptions
        {
            KhrExchangeRate = khrRate,
        });

        var razorEngine = new Mock<IRazorViewEngine>();
        var tempData    = new Mock<ITempDataProvider>();
        var sp          = new Mock<IServiceProvider>();

        return new ReceiptPrinterService(
            razorEngine.Object,
            tempData.Object,
            sp.Object,
            receiptOpts,
            paymentOpts,
            NullLogger<ReceiptPrinterService>.Instance);
    }

    // ── Arbitrary / generator ─────────────────────────────────────────────────

    /// <summary>
    /// Generates <see cref="SaleDetailDto"/> values with at least one line item.
    ///
    /// Constraints applied to keep generators realistic:
    /// <list type="bullet">
    ///   <item>InvoiceNo: non-empty alphanumeric string prefixed with "INV-".</item>
    ///   <item>Items: 1–5 items, each with positive quantity and non-negative prices.</item>
    ///   <item>Total: sum of item LineTotals, kept as a pre-computed positive decimal.</item>
    ///   <item>ItemName: drawn from a realistic word pool (never empty).</item>
    /// </list>
    /// </summary>
    public static Arbitrary<SaleDetailDto> SaleDetailArb()
    {
        // ── InvoiceNo ──────────────────────────────────────────────────────────
        var invoiceNoGen = Gen.Choose(1, 99999).Select(n => $"INV-{n:D5}");

        // ── SaleItem names ─────────────────────────────────────────────────────
        // Use a small fixed word pool so names are always non-empty and readable.
        var itemNameGen = Gen.Elements(
            "Apple Juice",
            "Bread Loaf",
            "Coffee Blend",
            "Drinking Water",
            "Energy Drink",
            "Fish Sauce",
            "Green Tea",
            "Honey Jar",
            "Instant Noodle",
            "Jasmine Rice");

        // ── UnitPrice: $0.01 – $999.99 ────────────────────────────────────────
        var unitPriceGen = Gen.Choose(1, 99999).Select(cents => (decimal)cents / 100m);

        // ── Quantity: 1 – 99 ─────────────────────────────────────────────────
        var qtyGen = Gen.Choose(1, 99);

        // ── Single SaleItemDto ────────────────────────────────────────────────
        var itemGen =
            from id        in Gen.Choose(1, 9999)
            from itemId    in Gen.Choose(1, 9999)
            from name      in itemNameGen
            from qty       in qtyGen
            from unitPrice in unitPriceGen
            let lineTotal  = Math.Round(unitPrice * qty, 2)
            select new SaleItemDto(
                SaleItemID: id,
                ItemID:     itemId,
                ItemCode:   $"CODE{id:D4}",
                ItemName:   name,
                Quantity:   qty,
                UnitPrice:  unitPrice,
                Discount:   0m,
                LineTotal:  lineTotal);

        // ── List of 1–5 items ─────────────────────────────────────────────────
        var itemListGen = Gen.Choose(1, 5).SelectMany(count => itemGen.ListOf(count));

        // ── SaleDetailDto ─────────────────────────────────────────────────────
        var saleGen =
            from saleId    in Gen.Choose(1, 99999)
            from invoiceNo in invoiceNoGen
            from items     in itemListGen
            let total      = items.Sum(i => i.LineTotal)
            select new SaleDetailDto(
                SaleID:        saleId,
                InvoiceNo:     invoiceNo,
                CreatedDate:   new DateTime(2024, 1, 15, 10, 30, 0),
                StaffUserName: "Staff01",
                CustomerName:  null,
                Total:         total,
                PaymentMethod: "Cash",
                Items:         items.ToList().AsReadOnly());

        return saleGen.ToArbitrary();
    }

    // ── Property 12: Receipt Completeness ────────────────────────────────────

    /// <summary>
    /// **Property 12: Receipt Completeness**
    ///
    /// For any <see cref="SaleDetailDto"/> with one or more <see cref="SaleItemDto"/>
    /// entries, the receipt generated by
    /// <see cref="ReceiptPrinterService.GenerateTextReceipt"/> SHALL contain:
    /// <list type="bullet">
    ///   <item><description>The <c>InvoiceNo</c></description></item>
    ///   <item><description>Each item's <c>ItemName</c> (first 16 characters — thermal truncation)</description></item>
    ///   <item><description>Each item's <c>Quantity</c></description></item>
    ///   <item><description>Each item's <c>UnitPrice</c></description></item>
    ///   <item><description>Each item's <c>LineTotal</c></description></item>
    ///   <item><description>The overall <c>Total</c></description></item>
    /// </list>
    ///
    /// None of these fields may be absent or empty in the rendered output.
    ///
    /// **Validates: Requirements 26.1, 26.2, 26.3**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(ReceiptPrinterTests) }, MaxTest = 200)]
    public Property Property12_ReceiptContainsAllRequiredFields(SaleDetailDto sale)
    {
        var svc    = BuildService();
        var receipt = svc.GenerateTextReceipt(sale);

        // ── 1. InvoiceNo ───────────────────────────────────────────────────────
        if (!receipt.Contains(sale.InvoiceNo))
        {
            return false.Label(
                $"Receipt missing InvoiceNo='{sale.InvoiceNo}'.");
        }

        // ── 2. Per-item fields ─────────────────────────────────────────────────
        foreach (var item in sale.Items)
        {
            // ItemName is printed truncated to 16 characters on the thermal line.
            // The spec says the receipt contains the item name; we verify the first
            // min(16, len) characters are present, matching the thermal truncation.
            var expectedNameFragment = item.ItemName.Length > 16
                ? item.ItemName[..16]
                : item.ItemName;

            if (!receipt.Contains(expectedNameFragment))
            {
                return false.Label(
                    $"Receipt missing ItemName fragment '{expectedNameFragment}' " +
                    $"(full name='{item.ItemName}').");
            }

            // Quantity appears as a plain integer in the receipt line.
            if (!receipt.Contains(item.Quantity.ToString()))
            {
                return false.Label(
                    $"Receipt missing Quantity={item.Quantity} for item '{item.ItemName}'.");
            }

            // UnitPrice appears formatted as "$X.XX".
            var unitPriceStr = item.UnitPrice.ToString("F2");
            if (!receipt.Contains(unitPriceStr))
            {
                return false.Label(
                    $"Receipt missing UnitPrice={unitPriceStr} for item '{item.ItemName}'.");
            }

            // LineTotal appears formatted as "$X.XX".
            var lineTotalStr = item.LineTotal.ToString("F2");
            if (!receipt.Contains(lineTotalStr))
            {
                return false.Label(
                    $"Receipt missing LineTotal={lineTotalStr} for item '{item.ItemName}'.");
            }
        }

        // ── 3. Overall Total ───────────────────────────────────────────────────
        var totalStr = sale.Total.ToString("F2");
        if (!receipt.Contains(totalStr))
        {
            return false.Label(
                $"Receipt missing overall Total={totalStr}.");
        }

        return true.ToProperty().Label(
            $"Receipt for InvoiceNo='{sale.InvoiceNo}' " +
            $"({sale.Items.Count} item(s), Total={sale.Total:F2}) is complete.");
    }
}
