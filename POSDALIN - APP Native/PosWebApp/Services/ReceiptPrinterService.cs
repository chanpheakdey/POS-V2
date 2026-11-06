using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using PosWebApp.Infrastructure;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Models.DTOs;
using PosWebApp.Models.ViewModels;
using System.Text;

namespace PosWebApp.Services;

/// <summary>
/// Receipt_Printer implementation.
///
/// <see cref="GenerateHtmlReceiptAsync"/> uses <see cref="IRazorViewEngine"/> +
/// <see cref="ITempDataProvider"/> to render <c>Shared/_Receipt.cshtml</c> into an
/// HTML string that the Checkout/Confirmation and Sale/Detail pages can embed directly.
///
/// <see cref="GenerateTextReceipt"/> produces a 32-column plain-text receipt suitable
/// for narrow thermal printers (80 mm paper).
///
/// Requirements: 26.1–26.12, 8.10
/// </summary>
public sealed class ReceiptPrinterService : IReceiptPrinterService
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ReceiptOptions _receiptOptions;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<ReceiptPrinterService> _logger;

    public ReceiptPrinterService(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider,
        IOptions<ReceiptOptions> receiptOptions,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<ReceiptPrinterService> logger)
    {
        _viewEngine       = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider  = serviceProvider;
        _receiptOptions   = receiptOptions.Value;
        _paymentOptions   = paymentOptions.Value;
        _logger           = logger;
    }

    // ── HTML receipt ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<string> GenerateHtmlReceiptAsync(SaleDetailDto sale)
    {
        var vm = BuildViewModel(sale);

        try
        {
            return await RenderViewToStringAsync("/Pages/Shared/_Receipt.cshtml", vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to render HTML receipt for SaleID={SaleID}", sale.SaleID);
            throw;
        }
    }

    // ── Plain-text receipt ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string GenerateTextReceipt(SaleDetailDto sale)
    {
        const int Width = 32; // columns for 80-mm thermal (typically 32 chars @ font-size 12)
        var (totalUsd, totalKhr) = CurrencyFormatter.Format(sale.Total, _paymentOptions.KhrExchangeRate);

        var sb = new StringBuilder();

        // ── Header ──────────────────────────────────────────────────────────
        sb.AppendLine(Center(_receiptOptions.BusinessName, Width));
        if (!string.IsNullOrEmpty(_receiptOptions.Address))
            sb.AppendLine(Center(_receiptOptions.Address, Width));
        if (!string.IsNullOrEmpty(_receiptOptions.Phone))
            sb.AppendLine(Center("Tel: " + _receiptOptions.Phone, Width));
        sb.AppendLine(new string('-', Width));

        // ── Sale metadata ────────────────────────────────────────────────────
        sb.AppendLine(LeftRight("Invoice:", sale.InvoiceNo, Width));
        sb.AppendLine(LeftRight("Sale ID:", "#" + sale.SaleID.ToString(), Width));
        sb.AppendLine(LeftRight("Date:", sale.CreatedDate.ToString("yyyy-MM-dd"), Width));
        sb.AppendLine(LeftRight("Time:", sale.CreatedDate.ToString("HH:mm"), Width));
        sb.AppendLine(LeftRight("Staff:", sale.StaffUserName, Width));
        if (!string.IsNullOrEmpty(sale.CustomerName))
            sb.AppendLine(LeftRight("Customer:", sale.CustomerName, Width));
        sb.AppendLine(new string('-', Width));

        // ── Column headers ───────────────────────────────────────────────────
        // Format: Item name (truncated) | Qty | Unit | Total
        sb.AppendLine("Item             Qty  Price  Total");
        sb.AppendLine(new string('-', Width));

        // ── Line items ───────────────────────────────────────────────────────
        foreach (var item in sale.Items)
        {
            // Truncate name to 16 chars; right-align qty, price, total
            var name      = Truncate(item.ItemName, 16).PadRight(16);
            var qty       = item.Quantity.ToString().PadLeft(3);
            var unitPrice = ("$" + item.UnitPrice.ToString("F2")).PadLeft(6);
            var lineTotal = ("$" + item.LineTotal.ToString("F2")).PadLeft(6);

            sb.AppendLine($"{name}{qty} {unitPrice} {lineTotal}");
        }

        sb.AppendLine(new string('-', Width));

        // ── Totals ───────────────────────────────────────────────────────────
        sb.AppendLine(LeftRight("Subtotal:", "$" + sale.Total.ToString("F2"), Width));
        sb.AppendLine(new string('=', Width));
        sb.AppendLine(LeftRight("TOTAL:", totalUsd, Width));
        sb.AppendLine(LeftRight("", totalKhr, Width));
        sb.AppendLine(new string('=', Width));

        // ── Footer ───────────────────────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine(Center("Thank you!", Width));
        sb.AppendLine(Center("*** RECEIPT ***", Width));

        return sb.ToString();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds the <see cref="ReceiptViewModel"/> shared by both output formats.
    /// </summary>
    private ReceiptViewModel BuildViewModel(SaleDetailDto sale)
    {
        var (totalUsd, totalKhr) = CurrencyFormatter.Format(sale.Total, _paymentOptions.KhrExchangeRate);

        return new ReceiptViewModel
        {
            BusinessName = _receiptOptions.BusinessName,
            Address      = _receiptOptions.Address,
            Phone        = _receiptOptions.Phone,
            SaleID       = sale.SaleID,
            InvoiceNo    = sale.InvoiceNo,
            CreatedDate  = sale.CreatedDate,
            StaffUserName = sale.StaffUserName,
            CustomerName = sale.CustomerName,
            Items        = sale.Items.Select(i => new ReceiptLineItem
            {
                ItemName  = i.ItemName,
                Quantity  = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
            }).ToArray(),
            Subtotal  = sale.Total,
            TotalUsd  = totalUsd,
            TotalKhr  = totalKhr,
        };
    }

    /// <summary>
    /// Uses <see cref="IRazorViewEngine"/> to render a Razor view to a string.
    /// Creates a minimal <see cref="ActionContext"/> backed by a <see cref="DefaultHttpContext"/>
    /// so the view engine can locate and compile the view without a real HTTP request.
    /// </summary>
    private async Task<string> RenderViewToStringAsync(string viewPath, object model)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider,
        };

        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        using var sw = new StringWriter();

        var viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewPath, isMainPage: false);

        if (!viewResult.Success)
        {
            var searchedLocations = string.Join(", ", viewResult.SearchedLocations);
            throw new InvalidOperationException(
                $"Could not find Razor view '{viewPath}'. Searched: {searchedLocations}");
        }

        var viewDictionary = new ViewDataDictionary(
            metadataProvider: new EmptyModelMetadataProvider(),
            modelState: new ModelStateDictionary())
        {
            Model = model,
        };

        var tempData = new TempDataDictionary(httpContext, _tempDataProvider);

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            tempData,
            sw,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);

        return sw.ToString();
    }

    // ── Text formatting helpers ───────────────────────────────────────────────

    private static string Center(string text, int width)
    {
        if (text.Length >= width) return text[..width];
        var totalPad = width - text.Length;
        var left     = totalPad / 2;
        return text.PadLeft(text.Length + left).PadRight(width);
    }

    private static string LeftRight(string leftText, string rightText, int width)
    {
        var combined = leftText + rightText;
        if (combined.Length >= width)
            return Truncate(combined, width);
        var spaces = width - combined.Length;
        return leftText + new string(' ', spaces) + rightText;
    }

    private static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength];
}
