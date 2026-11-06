using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Models.ViewModels;
using PosWebApp.Resources;
using PosWebApp.Services;
using PosWebApp.Models.DTOs;

namespace PosWebApp.Pages;

/// <summary>
/// Home page — displays the category grid.
///
/// **Validates: Requirements 3.1, 3.4, 3.5, 3.6, 3.7, 33.1**
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IProductCatalogService _catalogService;
    private readonly CatalogOptions _catalogOptions;
    private readonly ILogger<IndexModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(
        IProductCatalogService catalogService,
        IOptions<CatalogOptions> catalogOptions,
        ILogger<IndexModel> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _catalogService = catalogService;
        _catalogOptions = catalogOptions.Value;
        _logger         = logger;
        _localizer      = localizer;
    }

    /// <summary>View model bound to the Razor template.</summary>
    public IndexViewModel ViewModel { get; private set; } = new();

    /// <summary>
    /// Fetches categories from the <see cref="IProductCatalogService"/> (cache-first)
    /// and maps them to <see cref="ItemCardViewModel"/> applying the color array
    /// cyclically per Requirement 3.5.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        _logger.LogInformation("Index.OnGetAsync — loading category grid");

        IReadOnlyList<CategoryDto> categories;
        try
        {
            categories = await _catalogService.GetCategoriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load categories from product catalog service");
            // Render an empty grid rather than an unhandled error page.
            ViewModel = new IndexViewModel();
            return Page();
        }

        var categoryViewModels = categories
            .Select((cat, index) => new ItemCardViewModel
            {
                Id              = cat.CategoryID,
                Name            = cat.CategoryName,
                ImageUrl        = cat.ImageUrl,
                BackgroundColor = _catalogOptions.GetColor(index), // Requirement 3.5 — cyclic color
                Href            = Url.Page("/Groups", new { categoryId = cat.CategoryID })!,
                CardType        = ItemCardType.Category,
            })
            .ToList();

        ViewModel = new IndexViewModel { Categories = categoryViewModels };

        return Page();
    }
}
