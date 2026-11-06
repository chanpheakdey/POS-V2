using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Models.ViewModels;
using PosWebApp.Resources;
using PosWebApp.Services;

namespace PosWebApp.Pages;

/// <summary>
/// Displays the group grid for a given <paramref name="categoryId"/>.
/// Each group card links to <c>/Items/{categoryId}/{groupId}</c>.
/// Requirements: 4.1, 4.3, 4.5, 4.7, 4.8
/// </summary>
[Authorize]
public class GroupsModel : PageModel
{
    private readonly IProductCatalogService _catalog;
    private readonly CatalogOptions _catalogOptions;
    private readonly ILogger<GroupsModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GroupsModel(
        IProductCatalogService catalog,
        IOptions<CatalogOptions> catalogOptions,
        ILogger<GroupsModel> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _catalog        = catalog;
        _catalogOptions = catalogOptions.Value;
        _logger         = logger;
        _localizer      = localizer;
    }

    // ── Page properties ──────────────────────────────────────────────────────

    /// <summary>The category ID passed in the route.</summary>
    public int CategoryId { get; private set; }

    /// <summary>
    /// Groups belonging to the selected category, mapped to <see cref="ItemCardViewModel"/>
    /// with <c>Href</c> pointing to the Items page for each group.
    /// </summary>
    public IReadOnlyList<ItemCardViewModel> Groups { get; private set; } =
        Array.Empty<ItemCardViewModel>();

    // ── Handler ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads groups for <paramref name="categoryId"/> from the catalog service
    /// (cache-first, ASMX fallback on miss) and maps them to card view models.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int categoryId)
    {
        CategoryId = categoryId;

        try
        {
            var rawGroups = await _catalog.GetGroupsAsync(categoryId);

            Groups = rawGroups
                .Select((g, index) => new ItemCardViewModel
                {
                    Id              = g.GroupID,
                    Name            = g.GroupName,
                    ImageUrl        = g.ImageUrl,
                    // Requirement 4.6 — clicking a group navigates to its item listing.
                    Href            = Url.Page("/Items", new { categoryId = g.CategoryID, groupId = g.GroupID })!,
                    BackgroundColor = _catalogOptions.GetColor(index), // Requirement 4.8 — cyclic color
                    CardType        = ItemCardType.Group,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load groups for categoryId={CategoryId}", categoryId);
            TempData["Error"] = "Unable to load groups. Please try again.";
        }

        return Page();
    }
}
