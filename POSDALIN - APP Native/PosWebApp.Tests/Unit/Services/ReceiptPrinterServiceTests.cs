using Microsoft.AspNetCore.Http;
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
/// Unit tests for <see cref="ReceiptPrinterService"/>.
///
/// <see cref="ReceiptPrinterService.GenerateTextReceipt"/> is tested end-to-end
/// since it has no external dependencies beyond options.
///
/// <see cref="ReceiptPrinterService.GenerateHtmlReceiptAsync"/> is not unit-tested
/// here because it requires a live Razor view engine; it is covered by integration
/// tests instead.
///
/// Requirements: 26.1–26.12
/// </summary>
public class ReceiptPrinterServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ReceiptPrinterService BuildService(
        string businessName = "Test Shop",
        string address = "1 Main St",
        string phone = "012 345 678",
        decimal khrRate = 4100m)
    {
        var receiptOpts = Options.Create(new ReceiptOptions
        {
            BusinessName = businessName,
            Address = address,
            Phone = phone,
        });

        var paymentOpts = Options.Create(new PaymentOptions
        {
            KhrExchangeRate = khrRate,
        });

        // For text receipt generation, view engine and tempdata are not used.
        var razorEngine = new Mock<IRazorViewEngine>();
        var tempData = new Mock<ITempDataProvider>();
        var sp = new Mock<IServiceProvider>();

        return new ReceiptPrinterService(
            razorEngine.Object,
            tempData.Object,
            sp.Object,
            receiptOpts,
            paymentOpts,
            NullLogger<ReceiptPrinterService>.Instance);
    }

    private static SaleDetailDto BuildSale(
        int saleId = 1001,
        string invoiceNo = "INV-20240101-001",
        string staffName = "John",
        string? customerName = "Jane Doe",
        decimal total = 25.50m,
        IReadOnlyList<SaleItemDto>? items = null)
    {
        items ??= new List<SaleItemDto>
        {
            new(1, 10, "A001", "Apple Juice", 2, 5.00m, 0m, 10.00m),
            new(2, 11, "B002", "Bread Loaf",  1, 2.50m, 0m,  2.50m),
        };

        return new SaleDetailDto(
            SaleID: saleId,
            InvoiceNo: invoiceNo,
            CreatedDate: new DateTime(2024, 1, 15, 10, 30, 0),
            StaffUserName: staffName,
            CustomerName: customerName,
            Total: total,
            PaymentMethod: "Cash",
            Items: items);
    }

    // ── GenerateTextReceipt — business info ───────────────────────────────────

    [Fact]
    public void GenerateTextReceipt_ContainsBusinessName()
    {
        var svc    = BuildService(businessName: "My Store");
        var sale   = BuildSale();
        var result = svc.GenerateTextReceipt(sale);

        Assert.Contains("My Store", result);
    }

    [Fact]
    public void GenerateTextReceipt_ContainsAddress()
    {
        var svc    = BuildService(address: "456 Commerce Rd");
        var sale   = BuildSale();
        var result = svc.GenerateTextReceipt(sale);

        Assert.Contains("456 Commerce Rd", result);
    }

    [Fact]
    public void GenerateTextReceipt_ContainsPhone()
    {
        var svc    = BuildService(phone: "023-999-888");
        var sale   = BuildSale();
        var result = svc.GenerateTextReceipt(sale);

        Assert.Contains("023-999-888", result);
    }

    // ── GenerateTextReceipt — sale metadata ───────────────────────────────────

    [Fact]
    public void GenerateTextReceipt_ContainsSaleID()
    {
        var svc    = BuildService();
        var sale   = BuildSale(saleId: 9999);
        var result = svc.GenerateTextReceipt(sale);

        Assert.Contains("9999", result);
    }

    [Fact]
    public void GenerateTextReceipt_ContainsInvoiceNo()
    {
        var svc    = BuildService();
        var sale   = BuildSale(invoiceNo: "INV-2024-XYZ");
        var result = svc.GenerateTextReceipt(sale);

        Assert.Contains("INV-2024-XYZ", result);
    }

    [Fact]
    public void GenerateTextReceipt_ContainsDate()
    {
        var svc    = BuildService();
        var sale   = BuildSale();
        var result = svc.GenerateTextReceipt(sale);

        // CreatedDate is 2024-01-15
        Assert.Contains("2024-01-15", result);
    }

    [Fact]
    public void GenerateTextReceipt_ContainsTime()
    {
        var svc    = BuildService();
        var sale   = BuildSale();
        var result = svc.GenerateTextReceipt(sale);

        // CreatedDate time is 10:30
        Assert.Contains("10:30", result);
    }

    [Fact]
    public void GenerateTextReceipt_ContainsStaffName()
    {
        var svc    = BuildService();
        var sale   = BuildSale(staffName: "Alice");
        var result = svc.GenerateTextReceipt(sale);

        Assert.Contains("Alice", result);
    }

    [Fact]
    public void GenerateTextReceipt_ContainsCustomerName_WhenPresent()
    {
        var svc    = BuildService();
        var sale   = BuildSale(customerName: "Bob Smith");
        var result = svc.GenerateTextReceipt(sale);

        Assert.Contains("Bob Smith", result);
    }

    [Fact]
    public void GenerateTextReceipt_OmitsCustomerLine_WhenNull()
    {
        var svc    = BuildService();
        var sale   = BuildSale(customerName: null);
        var result = svc.GenerateTextReceipt(sale);

        // "Customer:" label should not appear when no customer is linked
        Assert.DoesNotContain("Customer:", result);
    }

    // ── GenerateTextReceipt — line items ──────────────────────────────────────

    [Fact]
    public void GenerateTextReceipt_ContainsLineItemNames()
    {
        var items = new List<SaleItemDto>
        {
            new(1, 1, "X01", "Widget Pro",  3, 10.00m, 0m, 30.00m),
            new(2, 2, "X02", "Gadget Plus", 1,  5.00m, 0m,  5.00m),
        };

        var sale   = BuildSale(items: items);
        var svc    = BuildService();
        var result = svc.GenerateTextReceipt(sale);

        // Names are truncated to 16 chars — "Widget Pro" fits
        Assert.Contains("Widget Pro", result);
        Assert.Contains("Gadget Plus", result);
    }

    [Fact]
    public void GenerateTextReceipt_ContainsLineItemQuantities()
    {
        var items = new List<SaleItemDto>
        {
            new(1, 1, "A01", "Product A", 7, 3.00m, 0m, 21.00m),
        };

        var sale   = BuildSale(items: items);
        var svc    = BuildService();
        var result = svc.GenerateTextReceipt(sale);

        Assert.Contains("7", result);
    }

    [Fact]
    public void GenerateTextReceipt_ContainsLineTotals()
    {
        var items = new List<SaleItemDto>
        {
            new(1, 1, "A01", "Product A", 2, 6.00m, 0m, 12.00m),
        };

        var sale   = BuildSale(items: items, total: 12.00m);
        var svc    = BuildService();
        var result = svc.GenerateTextReceipt(sale);

        Assert.Contains("12.00", result);
    }

    // ── GenerateTextReceipt — totals ──────────────────────────────────────────

    [Fact]
    public void GenerateTextReceipt_ContainsGrandTotalInUsd()
    {
        var svc    = BuildService(khrRate: 4100m);
        var sale   = BuildSale(total: 10.00m);
        var result = svc.GenerateTextReceipt(sale);

        Assert.Contains("$10.00", result);
    }

    [Fact]
    public void GenerateTextReceipt_ContainsGrandTotalInKhr()
    {
        // 10.00 × 4100 = 41000
        var svc    = BuildService(khrRate: 4100m);
        var sale   = BuildSale(total: 10.00m);
        var result = svc.GenerateTextReceipt(sale);

        // CurrencyFormatter formats as "41,000៛"
        Assert.Contains("41,000", result);
        Assert.Contains("៛", result);
    }

    // ── GenerateTextReceipt — empty item list ─────────────────────────────────

    [Fact]
    public void GenerateTextReceipt_HandlesEmptyItemList()
    {
        var sale   = BuildSale(items: new List<SaleItemDto>(), total: 0m);
        var svc    = BuildService();
        var result = svc.GenerateTextReceipt(sale);

        // Should still produce a receipt without throwing
        Assert.NotEmpty(result);
        Assert.Contains("TOTAL:", result);
    }
}
