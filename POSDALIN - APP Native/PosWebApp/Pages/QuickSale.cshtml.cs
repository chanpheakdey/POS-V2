using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PosWebApp.Infrastructure;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Models.DTOs;
using PosWebApp.Models.ViewModels;
using PosWebApp.Services;

namespace PosWebApp.Pages;

/// <summary>
/// Quick Sale mode — simplified interface for express transactions.
/// Supports barcode scanner input, a running cart total, and a number pad for quantity entry.
///
/// **Validates: Requirements 33.1, 33.2, 33.3, 33.4, 33.5, 33.6, 33.7, 33.8, 33.9, 33.10, 33.11**
/// </summary>
[Authorize]
[ValidateAntiForgeryToken]
public class QuickSaleModel : PageModel
{
    private readonly IProductCatalogService _catalog;
    private readonly ICartService _cart;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<QuickSaleModel> _logger;

    public QuickSaleModel(
        IProductCatalogService catalog,
        ICartService cart,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<QuickSaleModel> logger)
    {
        _catalog        = catalog;
        _cart           = cart;
        _paymentOptions = paymentOptions.Value;
        _logger         = logger;
    }

    // ── Page state ────────────────────────────────────────────────────────────

    /// <summary>Cart items mapped for display (Req 33.5).</summary>
    public IReadOnlyList<CartItemViewModel> CartItems { get; private set; } =
        Array.Empty<CartItemViewModel>();

    /// <summary>Running total formatted as USD (Req 33.5).</summary>
    public string TotalUsdText { get; private set; } = "$0.00";

    /// <summary>Running total formatted as KHR (Req 33.5).</summary>
    public string TotalKhrText { get; private set; } = "0៛";

    /// <summary>KHR exchange rate exposed for client-side formatting.</summary>
    public double KhrExchangeRate => _paymentOptions.KhrExchangeRate;

    /// <summary>Brief status message shown after a scan attempt.</summary>
    public string? ScanMessage { get; private set; }

    /// <summary>True when the last scan resulted in an error.</summary>
    public bool ScanError { get; private set; }

    // ── GET handler ───────────────────────────────────────────────────────────

    /// <summary>
    /// Renders the quick sale page with the current cart state (Req 33.2).
    /// </summary>
    public IActionResult OnGet()
    {
        BuildViewModel();
        return Page();
    }

    // ── POST: scan barcode (AJAX) — Req 33.3, 33.4 ───────────────────────────

    /// <summary>
    /// Looks up an item by its barcode / ItemCode, then auto-adds 1 unit to the cart.
    /// Returns JSON so quicksale.js can update the cart table without a full reload.
    ///
    /// Route: POST /QuickSale?handler=Scan
    /// </summary>
    public async Task<IActionResult> OnPostScanAsync([FromForm] string? barcode, [FromForm] int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return new JsonResult(new { success = false, error = "Barcode is required." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var code    = barcode.Trim();
        var userId  = User.FindFirstValue("UserID") ?? string.Empty;

        try
        {
            // Req 33.3 — look up item by ItemCode using SearchItemsAsync
            var results = await _catalog.SearchItemsAsync(code, page: 1, pageSize: 10);

            // Find an exact ItemCode match first, then fall back to the first result
            var item = results.FirstOrDefault(i =>
                            string.Equals(i.ItemCode, code, StringComparison.OrdinalIgnoreCase))
                        ?? results.FirstOrDefault();

            if (item is null)
            {
                return new JsonResult(new
                {
                    success = false,
                    error   = $"Item not found: {code}"
                });
            }

            // Req 33.4 — auto-add the scanned item to the cart
            var tiers = await _catalog.GetPriceTiersAsync(item.ItemID);

            var request = new AddToCartRequest
            {
                ItemId   = item.ItemID,
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                ImageUrl = item.ImageUrl,
                Quantity = quantity > 0 ? quantity : 1,
            };

            var cartItem = await _cart.AddItemAsync(HttpContext.Session, request, tiers, userId);
            var cart     = _cart.GetCart(HttpContext.Session);

            var (totalUsd, totalKhr) = CurrencyFormatter.Format(cart.Total, _paymentOptions.KhrExchangeRate);
            var (lineUsd, _)         = CurrencyFormatter.Format(cartItem.LineTotal, _paymentOptions.KhrExchangeRate);
            var (unitUsd, _)         = CurrencyFormatter.Format(cartItem.UnitPrice, _paymentOptions.KhrExchangeRate);

            return new JsonResult(new
            {
                success      = true,
                itemId       = cartItem.ItemId,
                itemCode     = cartItem.ItemCode,
                itemName     = cartItem.ItemName,
                quantity     = cartItem.Quantity,
                unitPriceUsd = unitUsd,
                lineTotalUsd = lineUsd,
                totalUsd,
                totalKhr,
                badgeCount   = cart.ItemCount,
                message      = $"Added: {item.ItemName}",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quick-sale scan failed for barcode={Barcode}", code);
            return new JsonResult(new { success = false, error = "Item lookup failed. Please try again." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    // ── POST: remove item from cart (AJAX) ────────────────────────────────────

    /// <summary>
    /// Removes a single item from the quick-sale cart.
    ///
    /// Route: POST /QuickSale?handler=Remove
    /// </summary>
    public async Task<IActionResult> OnPostRemoveAsync([FromForm] int itemId)
    {
        if (itemId <= 0)
        {
            return new JsonResult(new { success = false, error = "Invalid item." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var userId = User.FindFirstValue("UserID") ?? string.Empty;

        try
        {
            await _cart.RemoveItemAsync(HttpContext.Session, itemId, userId);
            var cart = _cart.GetCart(HttpContext.Session);

            var (totalUsd, totalKhr) = CurrencyFormatter.Format(cart.Total, _paymentOptions.KhrExchangeRate);

            return new JsonResult(new
            {
                success    = true,
                totalUsd,
                totalKhr,
                badgeCount = cart.ItemCount,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quick-sale remove failed for itemId={ItemId}", itemId);
            return new JsonResult(new { success = false, error = "Failed to remove item." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void BuildViewModel()
    {
        var cart = _cart.GetCart(HttpContext.Session);

        CartItems = cart.Items.Select(ci =>
        {
            var (lineUsd, _) = CurrencyFormatter.Format(ci.LineTotal, _paymentOptions.KhrExchangeRate);
            var (unitUsd, _) = CurrencyFormatter.Format(ci.UnitPrice, _paymentOptions.KhrExchangeRate);

            return new CartItemViewModel
            {
                ItemId       = ci.ItemId,
                ItemCode     = ci.ItemCode,
                ItemName     = ci.ItemName,
                Quantity     = ci.Quantity,
                UnitPriceUsd = unitUsd,
                LineTotalUsd = lineUsd,
                Discount     = ci.Discount,
                OriginalLineTotalUsd = CurrencyFormatter
                    .Format(ci.OriginalLineTotal, _paymentOptions.KhrExchangeRate).usd,
            };
        }).ToList();

        var (usd, khr) = CurrencyFormatter.Format(cart.Total, _paymentOptions.KhrExchangeRate);
        TotalUsdText   = usd;
        TotalKhrText   = khr;
    }
}
