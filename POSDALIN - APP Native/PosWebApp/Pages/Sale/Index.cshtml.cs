using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Models.DTOs;
using PosWebApp.Resources;
using PosWebApp.Services;
using System.Security.Claims;
using X.PagedList;

namespace PosWebApp.Pages.Sale;

/// <summary>
/// Sale history page — displays a paged, filterable list of sales.
///
/// Staff users only see their own sales; Managers and Admins see all sales
/// and can also filter by staff member.
///
/// Requirements: 9.5, 9.6, 9.7, 9.8, 9.9, 9.10, 9.11, 9.12, 28.6, 29.1, 29.2
/// </summary>
[Authorize]
public class SaleIndexModel : PageModel
{
    private readonly ISaleManagerService _saleManager;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<SaleIndexModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    private const int DefaultPageSize = 15;

    public SaleIndexModel(
        ISaleManagerService saleManager,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<SaleIndexModel> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _saleManager    = saleManager;
        _paymentOptions = paymentOptions.Value;
        _logger         = logger;
        _localizer      = localizer;
    }

    /// <summary>Current page number (1-based). Named PageNumber to avoid shadowing PageModel.Page().</summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Optional start date filter (inclusive).</summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    /// <summary>Optional end date filter (inclusive).</summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    /// <summary>Optional customer name filter.</summary>
    [BindProperty(SupportsGet = true)]
    public string? CustomerFilter { get; set; }

    // ── Page properties ───────────────────────────────────────────────────────

    /// <summary>Paged sale list displayed in the table.</summary>
    public IPagedList<SaleListItemDto> Sales { get; private set; } =
        new StaticPagedList<SaleListItemDto>(Array.Empty<SaleListItemDto>(), 1, DefaultPageSize, 0);

    /// <summary>
    /// True when the current user is Manager or Admin and can see the staff filter column
    /// and filter all sales across staff.
    /// Requirements: 9.7, 9.11
    /// </summary>
    public bool CanFilterByStaff { get; private set; }

    // ── GET handler ───────────────────────────────────────────────────────────

    /// <summary>
    /// Loads a paged list of sales, applying role-based and date/customer filters.
    ///
    /// Staff: always filtered to their own sales (StaffUserID = own ID).
    /// Manager/Admin: can see all sales; StaffUserID is null in query.
    ///
    /// Requirements: 9.5, 9.6, 9.7, 9.8, 9.9, 9.10, 9.11, 9.12
    /// </summary>
    public async Task OnGetAsync()
    {
        var userIdRaw = User.FindFirstValue("UserID");
        var userLevel = User.FindFirstValue("UserLevel") ?? "Staff";

        int? staffUserId = null;
        if (string.Equals(userLevel, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            // Staff can only see their own sales — enforce here (Requirement 9.7)
            if (int.TryParse(userIdRaw, out var ownId))
                staffUserId = ownId;
        }
        // Manager/Admin: leave staffUserId null so the service returns all sales (Req 9.11)

        CanFilterByStaff = !string.Equals(userLevel, "Staff", StringComparison.OrdinalIgnoreCase);

        // Clamp page number to valid range
        if (PageNumber < 1) PageNumber = 1;

        var query = new SaleHistoryQuery(
            StaffUserID: staffUserId,
            UserLevel:   userLevel,
            StartDate:   StartDate,
            EndDate:     EndDate?.Date.AddDays(1).AddSeconds(-1), // include full end day
            Page:        PageNumber,
            PageSize:    DefaultPageSize);

        try
        {
            var result = await _saleManager.GetSaleHistoryAsync(query);

            // Apply optional client-side customer name filter if the backend doesn't support it
            var items = string.IsNullOrWhiteSpace(CustomerFilter)
                ? result.Items
                : result.Items
                    .Where(s => s.CustomerName != null &&
                                s.CustomerName.Contains(CustomerFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            // StaticPagedList is correct here: data is already paged server-side;
            // we supply the full TotalCount so X.PagedList renders accurate pagination links.
            Sales = new StaticPagedList<SaleListItemDto>(
                items,
                PageNumber,
                DefaultPageSize,
                result.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve sale history (PageNumber={PageNumber})", PageNumber);
            // Sales stays as the empty StaticPagedList initialised above
        }
    }
}
