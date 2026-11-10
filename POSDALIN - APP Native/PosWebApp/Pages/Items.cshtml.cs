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

namespace PosWebApp.Pages;

/// <summary>
/// Displays the item grid for a given category and optional group.
///
/// When <c>groupId</c> is absent the page shows items directly for the
/// category (categories that have no groups — Requirement 4.2).
/// When <c>groupId</c> is supplied it shows items scoped to that group
/// (Requirements 4.1, 4.4).
///
/// Each item card carries a <c>data-item-id</c> attribute so the client-side
/// JavaScript can open the item detail modal (Requirement 5.1).
///
/// The <c>OnGetDetailAsync</c> handler returns the <c>_ItemDetailModal</c>
/// partial HTML for AJAX requests from <c>showItemDetail(itemId)</c> in JS.
///
/// Requirements: 4.1, 4.2, 4.3, 4.4, 4.6, 4.7, 4.8, 5.1–5.11
/// </summary>
[Authorize]
public class ItemsModel : PageModel
{
    private readonly IProductCatalogService _catalog;
    private readonly ICartService _cart;
    private readonly IImageOptimizerService _imageOptimizer;
    private readonly CatalogOptions _catalogOptions;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<ItemsModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ItemsModel(
        IProductCatalogService catalog,
        ICartService cart,
        IImageOptimizerService imageOptimizer,
        IOptions<CatalogOptions> catalogOptions,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<ItemsModel> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _catalog        = catalog;
        _cart           = cart;
        _imageOptimizer = imageOptimizer;
        _catalogOptions = catalogOptions.Value;
        _paymentOptions = paymentOptions.Value;
        _logger         = logger;
        _localizer      = localizer;
    }

    // ── Page properties ──────────────────────────────────────────────────────

    /// <summary>Category ID from the route.</summary>
    public int CategoryId { get; private set; }

    /// <summary>
    /// Optional group ID from the route.
    /// <see langword="null"/> means the category has no groups; items are
    /// retrieved using groupId=0 (getSaleItem with no group filter).
    /// </summary>
    public int? GroupId { get; private set; }

    /// <summary>
    /// Items to display, enriched with cart quantity badge count and color.
    /// Each entry has <c>Href = "#"</c> so the anchor in <c>_ItemCardPartial</c>
    /// does not navigate; the <c>data-item-id</c> button wrapper triggers the modal.
    /// </summary>
    public IReadOnlyList<ItemCardViewModel> Items { get; private set; } =
        Array.Empty<ItemCardViewModel>();

    // ── Handler ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads items for the given category/group combination.
    /// <paramref name="groupId"/> is optional; when absent, groupId=0 is passed
    /// to <see cref="IProductCatalogService.GetItemsAsync"/> which maps to the
    /// getSaleItem endpoint with no group filter (Requirement 4.2).
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int categoryId, int? groupId)
    {
        CategoryId = categoryId;
        GroupId    = groupId;

        // Read the current cart so we can display per-item quantity badges
        // (Requirements 6.3, 6.4).
        var cartState = _cart.GetCart(HttpContext.Session);

        try
        {
            // Requirement 4.1 / 4.2 — pass groupId (or 0 for "no group") to the
            // catalog service; it calls the getSaleItem ASMX endpoint with
            // CategoryID and GroupID parameters (Requirement 4.3, 4.4).
            var rawItems = await _catalog.GetItemsAsync(categoryId, groupId ?? 0);

            Items = rawItems
                .Select((item, index) =>
                {
                    var cartQty = cartState.Items
                        .FirstOrDefault(ci => ci.ItemId == item.ItemID)
                        ?.Quantity;

                    return new ItemCardViewModel
                    {
                        Id              = item.ItemID,
                        Name            = item.ItemName,
                        ItemCode        = item.ItemCode,
                        ImageUrl        = item.ImageUrl,
                        Price           = item.Price,
                        BackgroundColor = _catalogOptions.GetColor(index), // Requirement 4.8 — cyclic color
                        // Href = "#" — item cards open the detail modal, not a page.
                        // The onclick handler in Items.cshtml calls showItemDetail(id).
                        Href            = "#",
                        CartQty         = cartQty > 0 ? cartQty : null, // badge shown when > 0
                        CardType        = ItemCardType.Item,
                    };
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to load items for categoryId={CategoryId} groupId={GroupId}",
                categoryId, groupId);
            TempData["Error"] = "Unable to load items. Please try again.";
        }

        return Page();
    }

    // ── AJAX handler ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <c>_ItemDetailModal</c> partial as an HTML fragment for
    /// AJAX injection by <c>showItemDetail(itemId)</c> in JavaScript.
    ///
    /// Route: GET /Items/Detail/{itemId}
    ///
    /// 1. Retrieves item detail from <see cref="IProductCatalogService.GetItemDetailAsync"/>.
    /// 2. Retrieves price tiers from <see cref="IProductCatalogService.GetPriceTiersAsync"/>.
    /// 3. Formats each tier in both USD and KHR using <see cref="CurrencyFormatter.Format"/>.
    /// 4. Builds a responsive srcset via <see cref="IImageOptimizerService.BuildSrcSet"/>.
    /// 5. Reads the current cart quantity for the item from session.
    /// 6. Returns <c>Partial("_ItemDetailModal", viewModel)</c> — NOT a full page.
    ///
    /// Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10, 5.11
    /// </summary>
    public async Task<IActionResult> OnGetDetailAsync(int itemId)
    {
        try
        {
            // Requirements 5.2 — retrieve item detail from cache/backend.
            // Requirements 5.3 — retrieve price tiers from cache/backend.
            var detailTask = _catalog.GetItemDetailAsync(itemId);
            var tiersTask  = _catalog.GetPriceTiersAsync(itemId);
            await Task.WhenAll(detailTask, tiersTask);

            var detail = await detailTask;
            var tiers  = await tiersTask;

            var exchangeRate = _paymentOptions.KhrExchangeRate;

            // Requirement 5.6 — format each tier in USD and KHR.
            var tierViewModels = tiers
                .OrderBy(t => t.FromQty)
                .Select(t =>
                {
                    var (usd, khr) = CurrencyFormatter.Format(t.Price, exchangeRate);
                    return new PriceTierViewModel
                    {
                        Id           = t.ID,
                        FromQty      = t.FromQty,
                        ToQty        = t.ToQty,
                        Price        = t.Price,
                        FormattedUsd = usd,
                        FormattedKhr = khr,
                    };
                })
                .ToList();

            // Requirement 5.7 — generate responsive srcset for full-resolution image.
            var srcSet = _imageOptimizer.BuildSrcSet(detail.ImageUrl, new[]
            {
                (400, 300),
                (600, 450),
                (800, 600),
            });

            // Proxy the primary image src through the /Images route so the browser
            // never tries to reach the internal backend URL directly.
            var proxiedImageUrl = string.IsNullOrWhiteSpace(detail.ImageUrl)
                ? string.Empty
                : $"/Images/{Uri.EscapeDataString(detail.ImageUrl)}?w=400&h=300";

            // Requirement 5.10 — read current cart quantity from session.
            var cartState    = _cart.GetCart(HttpContext.Session);
            var cartQty      = cartState.Items
                .FirstOrDefault(ci => ci.ItemId == itemId)
                ?.Quantity ?? 0;

            // Requirement 5.11 — pass item detail via ViewData / model to partial.
            var viewModel = new ItemDetailViewModel
            {
                ItemId               = detail.ItemID,
                ItemCode             = detail.ItemCode,
                ItemName             = detail.ItemName,
                ImageUrl             = proxiedImageUrl,
                SrcSet               = srcSet,
                PriceTiers           = tierViewModels,
                CurrentCartQuantity  = cartQty,
            };

            return Partial("_ItemDetailModal", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load item detail for itemId={ItemId}", itemId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Unable to load item details. Please try again.");
        }
    }
}
