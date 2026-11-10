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
/// Unified catalog page with categories in top bar, groups in left sidebar,
/// and all items displayed without pagination.
/// </summary>
[Authorize]
public class CatalogModel : PageModel
{
    private readonly IProductCatalogService _catalog;
    private readonly ICartService _cart;
    private readonly IImageOptimizerService _imageOptimizer;
    private readonly CatalogOptions _catalogOptions;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<CatalogModel> _logger;

    public CatalogModel(
        IProductCatalogService catalog,
        ICartService cart,
        IImageOptimizerService imageOptimizer,
        IOptions<CatalogOptions> catalogOptions,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<CatalogModel> logger)
    {
        _catalog = catalog;
        _cart = cart;
        _imageOptimizer = imageOptimizer;
        _catalogOptions = catalogOptions.Value;
        _paymentOptions = paymentOptions.Value;
        _logger = logger;
    }

    // ── Page Properties ──────────────────────────────────────────────────────

    /// <summary>All available categories for the left sidebar.</summary>
    public IReadOnlyList<ItemCardViewModel> Categories { get; private set; } =
        Array.Empty<ItemCardViewModel>();

    /// <summary>Items grouped by GroupName.</summary>
    public IReadOnlyList<ItemGroupViewModel> ItemGroups { get; private set; } =
        Array.Empty<ItemGroupViewModel>();

    /// <summary>Currently selected category ID.</summary>
    public int SelectedCategoryId { get; private set; }
    
    /// <summary>View model for items grouped by group name.</summary>
    public class ItemGroupViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = "";
        public IReadOnlyList<ItemCardViewModel> Items { get; set; } = Array.Empty<ItemCardViewModel>();
    }

    // ── Handler ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads categories, groups, and items based on query parameters.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int? categoryId)
    {
        try
        {
            // Load all categories for the left sidebar
            var rawCategories = await _catalog.GetCategoriesAsync();
            Categories = rawCategories
                .Select((cat, index) => new ItemCardViewModel
                {
                    Id = cat.CategoryID,
                    Name = cat.CategoryName,
                    ImageUrl = cat.ImageUrl,
                    BackgroundColor = _catalogOptions.GetColor(index),
                    Href = $"?categoryId={cat.CategoryID}",
                    CardType = ItemCardType.Category,
                })
                .ToList();

            // If no category selected, select the first one
            if (!categoryId.HasValue && Categories.Any())
            {
                categoryId = Categories.First().Id;
            }

            SelectedCategoryId = categoryId ?? 0;

            if (SelectedCategoryId > 0)
            {
                // Load ALL products from catalog to get GroupName
                var allProducts = await _catalog.GetAllProductsAsync();
                
                // Filter by selected category only (show all groups)
                var filteredProducts = allProducts
                    .Where(p => p.CategoryID == SelectedCategoryId)
                    .ToList();
                
                // Read the current cart for quantity badges
                var cartState = _cart.GetCart(HttpContext.Session);

                // Group items by GroupName
                ItemGroups = filteredProducts
                    .GroupBy(p => new { p.GroupID, p.GroupName })
                    .OrderBy(g => g.Key.GroupName)
                    .Select(group => new ItemGroupViewModel
                    {
                        GroupId = group.Key.GroupID,
                        GroupName = group.Key.GroupName,
                        Items = group.Select((item, index) =>
                        {
                            var cartQty = cartState.Items
                                .FirstOrDefault(ci => ci.ItemId == item.ItemID)
                                ?.Quantity;

                            return new ItemCardViewModel
                            {
                                Id = item.ItemID,
                                Name = item.ItemName,
                                ItemCode = item.ItemCode,
                                ImageUrl = item.ImageUrl,
                                Price = item.Price,
                                BackgroundColor = _catalogOptions.GetColor(index),
                                Href = "#",
                                CartQty = cartQty > 0 ? cartQty : null,
                                CardType = ItemCardType.Item,
                            };
                        }).ToList()
                    })
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load catalog data");
            TempData["Error"] = "Unable to load catalog. Please try again.";
        }

        return Page();
    }

    // ── AJAX Handler for Item Detail ─────────────────────────────────────────

    /// <summary>
    /// Returns the item detail modal partial as HTML fragment.
    /// Route: GET /Catalog?handler=Detail&itemId={itemId}
    /// </summary>
    public async Task<IActionResult> OnGetDetailAsync(int itemId)
    {
        try
        {
            var detailTask = _catalog.GetItemDetailAsync(itemId);
            var tiersTask = _catalog.GetPriceTiersAsync(itemId);
            await Task.WhenAll(detailTask, tiersTask);

            var detail = await detailTask;
            var tiers = await tiersTask;

            var exchangeRate = _paymentOptions.KhrExchangeRate;

            var tierViewModels = tiers
                .OrderBy(t => t.FromQty)
                .Select(t =>
                {
                    var (usd, khr) = CurrencyFormatter.Format(t.Price, exchangeRate);
                    return new PriceTierViewModel
                    {
                        Id = t.ID,
                        FromQty = t.FromQty,
                        ToQty = t.ToQty,
                        Price = t.Price,
                        FormattedUsd = usd,
                        FormattedKhr = khr,
                    };
                })
                .ToList();

            // Build image URL from ItemID if empty
            var imageUrl = string.IsNullOrWhiteSpace(detail.ImageUrl)
                ? $"/Uploads/Small/{detail.ItemID}.jpg"
                : detail.ImageUrl;

            var srcSet = _imageOptimizer.BuildSrcSet(imageUrl, new[]
            {
                (400, 300),
                (600, 450),
                (800, 600),
            });

            var cartState = _cart.GetCart(HttpContext.Session);
            var cartQty = cartState.Items
                .FirstOrDefault(ci => ci.ItemId == itemId)
                ?.Quantity ?? 0;

            var viewModel = new ItemDetailViewModel
            {
                ItemId = detail.ItemID,
                ItemCode = detail.ItemCode,
                ItemName = detail.ItemName,
                ImageUrl = imageUrl,
                SrcSet = srcSet,
                PriceTiers = tierViewModels,
                CurrentCartQuantity = cartQty,
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
