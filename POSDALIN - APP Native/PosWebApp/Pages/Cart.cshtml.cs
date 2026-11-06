using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using PosWebApp.Infrastructure;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Models.ViewModels;
using PosWebApp.Resources;
using PosWebApp.Services;
using System.Security.Claims;

namespace PosWebApp.Pages;

/// <summary>
/// Cart view and management page.
/// Displays all items in the current session cart, supports quantity
/// adjustment (AJAX via cart.js), per-item removal, and full-cart clear.
///
/// Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.6, 17.7, 17.8,
///               17.9, 17.10, 17.11, 17.12,
///               28.1, 28.2, 28.3, 28.4, 28.5, 28.6, 28.7, 28.8, 28.9, 28.10, 28.11
/// </summary>
[Authorize]
[ValidateAntiForgeryToken]
public class CartModel : PageModel
{
    private readonly ICartService _cart;
    private readonly IProductCatalogService _catalog;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<CartModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CartModel(
        ICartService cart,
        IProductCatalogService catalog,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<CartModel> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _cart           = cart;
        _catalog        = catalog;
        _paymentOptions = paymentOptions.Value;
        _logger         = logger;
        _localizer      = localizer;
    }

    // ── Page properties ──────────────────────────────────────────────────────

    /// <summary>
    /// View model built from the current session cart.
    /// Requirement 17.2
    /// </summary>
    public CartViewModel ViewModel { get; private set; } = new();

    /// <summary>
    /// Exposes the configured KHR exchange rate to the Razor view so that
    /// cart.js can perform client-side KHR calculations after AJAX updates.
    /// </summary>
    public decimal KhrExchangeRate => _paymentOptions.KhrExchangeRate;

    /// <summary>Bound model for AJAX add-to-cart. Requirement 6.12</summary>
    [BindProperty]
    public AddToCartRequest AddInput { get; set; } = new();

    /// <summary>Bound model for applying a discount. Requirements 28.3, 28.4</summary>
    [BindProperty]
    public ApplyDiscountRequest DiscountInput { get; set; } = new();

    // ── GET handler ──────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the current cart from session, maps it to <see cref="CartViewModel"/>
    /// and formats totals in both USD and KHR.
    ///
    /// Requirement 17.1, 17.2, 17.7
    /// </summary>
    public Task<IActionResult> OnGetAsync()
    {
        BuildViewModel();
        return Task.FromResult<IActionResult>(Page());
    }

    // ── POST: Add item (AJAX endpoint) ────────────────────────────────────────

    /// <summary>
    /// Adds an item to the cart via AJAX POST. Called by cart.js <c>addToCart()</c>.
    /// Returns JSON so the browser can update the badge and show a toast without
    /// a full page reload.
    ///
    /// Requirement 6.12
    /// </summary>
    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid || AddInput.ItemId == 0)
        {
            return new JsonResult(new { success = false, error = "Invalid item data." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var userId = User.FindFirstValue("UserID") ?? string.Empty;

        try
        {
            var tiers = await _catalog.GetPriceTiersAsync(AddInput.ItemId);
            var item  = await _cart.AddItemAsync(HttpContext.Session, AddInput, tiers, userId);

            var cart = _cart.GetCart(HttpContext.Session);

            return new JsonResult(new
            {
                success    = true,
                newQty     = item.Quantity,
                badgeCount = cart.ItemCount,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to add itemId={ItemId} to cart", AddInput.ItemId);
            return new JsonResult(new { success = false, error = "Failed to add item to cart." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    // ── POST: Update quantity (AJAX endpoint) ─────────────────────────────────

    /// <summary>
    /// Updates the quantity of a single cart item via AJAX.
    /// Returns a JSON response consumed by cart.js for quantity updates.
    /// If <paramref name="quantity"/> is zero or less the item is removed.
    ///
    /// Requirement 17.4, 17.12
    /// </summary>
    public async Task<IActionResult> OnPostUpdateAsync(int itemId, int quantity)
    {
        var userId = User.FindFirstValue("UserID") ?? string.Empty;

        try
        {
            // Re-resolve tiers so the price updates correctly when qty crosses
            // a tier boundary (Req 17.4).
            var tiers = await _catalog.GetPriceTiersAsync(itemId);

            var updated = await _cart.UpdateQuantityAsync(
                HttpContext.Session, itemId, quantity, tiers, userId);

            var cart  = _cart.GetCart(HttpContext.Session);
            var total = cart.Total;
            var (totalUsd, totalKhr) = CurrencyFormatter.Format(total, _paymentOptions.KhrExchangeRate);

            var (lineTotalUsd, _) = updated is not null
                ? CurrencyFormatter.Format(updated.LineTotal, _paymentOptions.KhrExchangeRate)
                : ("$0.00", "0៛");

            return new JsonResult(new
            {
                success       = true,
                badgeCount    = cart.ItemCount,
                removed       = updated is null,
                newQty        = updated?.Quantity ?? 0,
                lineTotal     = updated?.LineTotal ?? 0m,
                lineTotalUsd,
                grandTotalUsd = totalUsd,
                grandTotalKhr = totalKhr,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to update quantity for itemId={ItemId} quantity={Quantity}",
                itemId, quantity);
            return new JsonResult(new { success = false, error = "Failed to update item." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    // ── POST: Apply discount (Manager/Admin only) ──────────────────────────────

    /// <summary>
    /// Applies a percentage or fixed discount to a specific cart item.
    /// Only Manager and Admin roles may call this handler (Req 28.1, 28.2).
    /// Discounts &gt; 20% additionally require an authorisation PIN/password (Req 28.7).
    /// Logs unauthorised access attempts (Req 28.8).
    ///
    /// Requirements: 28.1, 28.2, 28.3, 28.4, 28.5, 28.6, 28.7, 28.8
    /// </summary>
    public IActionResult OnPostApplyDiscount()
    {
        // Requirement 28.1, 28.2 — role check; log unauthorized attempts (Req 28.8)
        if (!User.IsInRole("Manager") && !User.IsInRole("Admin"))
        {
            var claimantId = User.FindFirstValue("UserID") ?? "unknown";
            _logger.LogWarning(
                "Unauthorized discount attempt by userId={UserId} on itemId={ItemId}",
                claimantId, DiscountInput.ItemId);
            return new JsonResult(new { success = false, error = "Access denied. Manager or Admin role required." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        // Requirement 28.3 — validate 0–100 range
        if (DiscountInput.DiscountPercent < 0 || DiscountInput.DiscountPercent > 100)
        {
            return new JsonResult(new { success = false, error = "Discount must be between 0 and 100 percent." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        // Requirement 28.7 — discounts > 20% require an authorising UserID
        if (DiscountInput.DiscountPercent > 20 && DiscountInput.AuthorizingUserId <= 0)
        {
            return new JsonResult(new { success = false, requiresAuth = true, error = "Discounts over 20% require manager authorisation." })
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity
            };
        }

        try
        {
            var authorizingUserId = DiscountInput.AuthorizingUserId > 0
                ? DiscountInput.AuthorizingUserId
                : int.TryParse(User.FindFirstValue("UserID"), out var uid) ? uid : 0;

            var updatedState = _cart.ApplyDiscount(
                HttpContext.Session,
                DiscountInput.ItemId,
                DiscountInput.DiscountPercent,
                DiscountInput.DiscountType,
                authorizingUserId);

            var updatedItem = updatedState.Items.FirstOrDefault(i => i.ItemId == DiscountInput.ItemId);
            if (updatedItem is null)
                return new JsonResult(new { success = false, error = "Item not found in cart." })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };

            var (lineTotalUsd, _) = CurrencyFormatter.Format(updatedItem.LineTotal, _paymentOptions.KhrExchangeRate);
            var (originalLineTotalUsd, _) = CurrencyFormatter.Format(updatedItem.OriginalLineTotal, _paymentOptions.KhrExchangeRate);
            var (totalUsd, totalKhr) = CurrencyFormatter.Format(updatedState.Total, _paymentOptions.KhrExchangeRate);

            _logger.LogInformation(
                "Discount applied: itemId={ItemId} discount={Discount}% type={DiscountType} by userId={UserId}",
                DiscountInput.ItemId, DiscountInput.DiscountPercent, DiscountInput.DiscountType, authorizingUserId);

            return new JsonResult(new
            {
                success             = true,
                discount            = updatedItem.Discount,
                discountType        = updatedItem.DiscountType,
                lineTotalUsd,
                originalLineTotalUsd,
                grandTotalUsd       = totalUsd,
                grandTotalKhr       = totalKhr,
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return new JsonResult(new { success = false, error = ex.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { success = false, error = ex.Message })
            {
                StatusCode = StatusCodes.Status404NotFound
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply discount to itemId={ItemId}", DiscountInput.ItemId);
            return new JsonResult(new { success = false, error = "Failed to apply discount." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    // ── POST: Remove single item ──────────────────────────────────────────────

    /// <summary>
    /// Removes the specified item from the cart and redirects back to the cart page.
    /// Called by the remove form in <c>_CartItemPartial.cshtml</c>.
    ///
    /// Requirement 17.5, 17.6
    /// </summary>
    public async Task<IActionResult> OnPostRemoveAsync(int itemId)
    {
        var userId = User.FindFirstValue("UserID") ?? string.Empty;

        try
        {
            await _cart.RemoveItemAsync(HttpContext.Session, itemId, userId);
            _logger.LogInformation(
                "Cart item removed: itemId={ItemId} by userId={UserId}", itemId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to remove itemId={ItemId} from cart", itemId);
            TempData["Error"] = "Unable to remove the item. Please try again.";
        }

        return RedirectToPage("/Cart");
    }

    // ── POST: Clear entire cart ───────────────────────────────────────────────

    /// <summary>
    /// Clears all items from the cart and redirects back to the cart page.
    ///
    /// Requirement 17.6
    /// </summary>
    public async Task<IActionResult> OnPostClearAsync()
    {
        var userId = User.FindFirstValue("UserID") ?? string.Empty;

        try
        {
            await _cart.ClearCartAsync(HttpContext.Session, userId);
            _logger.LogInformation("Cart cleared by userId={UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cart for userId={UserId}", userId);
            TempData["Error"] = "Unable to clear the cart. Please try again.";
        }

        return RedirectToPage("/Cart");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds <see cref="ViewModel"/> from the current session cart state.
    /// Pre-formats USD strings so the Razor view is free of formatting logic.
    /// </summary>
    private void BuildViewModel()
    {
        var state  = _cart.GetCart(HttpContext.Session);
        var rate   = _paymentOptions.KhrExchangeRate;

        var items = state.Items.Select(ci =>
        {
            var (lineTotalUsd, _)         = CurrencyFormatter.Format(ci.LineTotal, rate);
            var (unitPriceUsd, _)         = CurrencyFormatter.Format(ci.UnitPrice, rate);
            var (originalLineTotalUsd, _) = CurrencyFormatter.Format(ci.OriginalLineTotal, rate);
            return new CartItemViewModel
            {
                ItemId               = ci.ItemId,
                ItemCode             = ci.ItemCode,
                ItemName             = ci.ItemName,
                Quantity             = ci.Quantity,
                UnitPriceUsd         = unitPriceUsd,
                LineTotalUsd         = lineTotalUsd,
                Discount             = ci.Discount,
                OriginalLineTotalUsd = originalLineTotalUsd,
            };
        }).ToList();

        var (totalUsd, totalKhr) = CurrencyFormatter.Format(state.Total, rate);

        ViewModel = new CartViewModel
        {
            Items        = items,
            TotalUsdText = totalUsd,
            TotalKhrText = totalKhr,
        };
    }
}
