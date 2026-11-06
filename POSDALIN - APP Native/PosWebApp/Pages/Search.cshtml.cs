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
/// Displays search results for items matching a query string.
/// (truncated — see class for full doc)
/// </summary>
[Authorize]
public class SearchModel : PageModel
{
    private readonly IProductCatalogService _catalog;
    private readonly ICartService _cart;
    private readonly ILogger<SearchModel> _logger;
    private readonly CatalogOptions _catalogOptions;
    private readonly IStringLocalizer<SharedResource> _localizer;

    // Pagination constants
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 48;
    private const int AutocompletePageSize = 10;
    private const int MinQueryLength = 2;

    public SearchModel(
        IProductCatalogService catalog,
        ICartService cart,
        IOptions<CatalogOptions> catalogOptions,
        ILogger<SearchModel> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _catalog        = catalog;
        _cart           = cart;
        _catalogOptions = catalogOptions.Value;
        _logger         = logger;
        _localizer      = localizer;
    }

    // ── Bound query parameter ─────────────────────────────────────────────────

    /// <summary>
    /// The search query string from <c>?q=</c>.
    /// Bound via Tag_Helper on the search form (Requirement 10.10).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    // ── Page state ────────────────────────────────────────────────────────────

    /// <summary>Item cards to display in the search result grid.</summary>
    public IReadOnlyList<ItemCardViewModel> Results { get; private set; } =
        Array.Empty<ItemCardViewModel>();

    /// <summary>
    /// <see langword="true"/> when a query was supplied but is shorter than
    /// <see cref="MinQueryLength"/>.  Renders a warning instead of empty results.
    /// </summary>
    public bool ShowMinLengthWarning { get; private set; }

    // ── Handlers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads search results for the current query.
    /// Redirects to home when the query is empty or whitespace (Requirement 10.6).
    /// Shows a warning when the query is shorter than 2 characters (Requirement 10.7).
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        // Req 10.6 — redirect to home when search is cleared
        if (string.IsNullOrWhiteSpace(Q))
        {
            return RedirectToPage("/Index");
        }

        var trimmed = Q.Trim();

        // Req 10.7 — minimum 2 characters enforced
        if (trimmed.Length < MinQueryLength)
        {
            ShowMinLengthWarning = true;
            return Page();
        }

        var cartState = _cart.GetCart(HttpContext.Session);

        try
        {
            // Req 10.2 — call Backend_Service searchItems endpoint via catalog service
            // Req 10.8 — pass page and pageSize parameters
            var rawItems = await _catalog.SearchItemsAsync(trimmed, DefaultPage, DefaultPageSize);

            Results = rawItems
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
                        BackgroundColor = _catalogOptions.GetColor(index),
                        // Href = "#" — item cards open the detail modal
                        Href            = "#",
                        CartQty         = cartQty > 0 ? cartQty : null,
                        CardType        = ItemCardType.Item,
                    };
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query={Query}", trimmed);
            TempData["Error"] = "Search is unavailable. Please try again.";
        }

        return Page();
    }

    /// <summary>
    /// Returns autocomplete suggestions as a JSON array of <c>{id, name, code}</c> objects.
    ///
    /// Route: GET /Search?handler=Autocomplete&amp;q={query}
    ///
    /// Returns an empty array when the query is shorter than 2 characters.
    /// Limits results to <see cref="AutocompletePageSize"/> suggestions.
    ///
    /// Requirement 10.9 — autocomplete handler that calls Backend_Service
    /// searchItems endpoint and returns JSON results.
    /// </summary>
    public async Task<IActionResult> OnGetAutocompleteAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < MinQueryLength)
        {
            return new JsonResult(Array.Empty<object>());
        }

        try
        {
            var items = await _catalog.SearchItemsAsync(q.Trim(), DefaultPage, AutocompletePageSize);

            var suggestions = items
                .Select(item => new
                {
                    id   = item.ItemID,
                    name = item.ItemName,
                    code = item.ItemCode,
                })
                .ToArray();

            return new JsonResult(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autocomplete failed for query={Query}", q);
            return new JsonResult(Array.Empty<object>());
        }
    }

    // ── Nested model used by _NavbarPartial.cshtml asp-for binding ────────────

    /// <summary>
    /// Minimal input model for the navbar search form's <c>asp-for</c> binding.
    /// Allows <c>_NavbarPartial.cshtml</c> to generate the correct <c>name</c> and
    /// <c>id</c> attributes for the search input without a full page model dependency.
    /// </summary>
    public class SearchInputModel
    {
        /// <summary>The search query string bound from <c>?q=</c>.</summary>
        public string? Q { get; set; }
    }
}
