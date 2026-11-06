using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using PosWebApp.Infrastructure;
using PosWebApp.Infrastructure.Exceptions;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Models.DTOs;
using PosWebApp.Resources;
using PosWebApp.Services;
using System.Security.Claims;

namespace PosWebApp.Pages.Sale;

/// <summary>
/// Sale detail page — displays the full details of a single sale including line items
/// and void status.  Managers and Admins can void an active sale from this page.
///
/// Requirements: 9.5, 9.6, 28.6, 29.1, 29.2, 29.8, 29.11
/// </summary>
[Authorize]
public class SaleDetailModel : PageModel
{
    private readonly ISaleManagerService _saleManager;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<SaleDetailModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SaleDetailModel(
        ISaleManagerService saleManager,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<SaleDetailModel> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _saleManager    = saleManager;
        _paymentOptions = paymentOptions.Value;
        _logger         = logger;
        _localizer      = localizer;
    }

    // ── Page properties ───────────────────────────────────────────────────────

    /// <summary>Full sale detail including line items.</summary>
    public SaleDetailDto Detail { get; private set; } = null!;

    /// <summary>Formatted total in USD, e.g. "$12.50".</summary>
    public string TotalUsd { get; private set; } = string.Empty;

    /// <summary>Formatted total in KHR, e.g. "51,250៛".</summary>
    public string TotalKhr { get; private set; } = string.Empty;

    /// <summary>Void reason posted from the form.</summary>
    [BindProperty]
    public string? VoidReason { get; set; }

    // ── GET ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the full sale detail for <paramref name="id"/> and formats the totals.
    /// Returns 404 when the sale is not found.
    ///
    /// Requirements: 9.5, 9.6, 28.6, 29.1
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Detail = await _saleManager.GetSaleDetailAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sale detail for SaleID={SaleID}", id);
            return NotFound();
        }

        if (Detail is null)
            return NotFound();

        FormatTotals();
        return Page();
    }

    // ── POST: Void sale ───────────────────────────────────────────────────────

    /// <summary>
    /// Voids the sale identified by <paramref name="id"/>.
    /// Only accessible to users in the Manager or Admin role.
    ///
    /// Validates that a non-empty reason is supplied, calls
    /// <see cref="ISaleManagerService.VoidSaleAsync"/>, then redirects back to
    /// the same page so the user sees the updated "VOIDED" status.
    ///
    /// Requirements: 29.2, 29.8, 29.11
    /// </summary>
    public async Task<IActionResult> OnPostVoidAsync(int id)
    {
        // Role check: only Manager or Admin may void (Requirement 29.2, 29.11)
        // [Authorize(Roles)] cannot be applied to handler methods — enforced inline instead.
        if (!User.IsInRole("Manager") && !User.IsInRole("Admin"))
        {
            // Requirement 28.8, 30.8 — log unauthorized access attempt
            var attemptingUserId = User.FindFirstValue("UserID") ?? "unknown";
            _logger.LogWarning(
                "Unauthorized void attempt by userId={UserId} on SaleID={SaleID}",
                attemptingUserId, id);
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(VoidReason))
        {
            ModelState.AddModelError(nameof(VoidReason), "Please enter a reason for voiding this sale.");
        }

        // Reload detail so the page can be re-rendered if validation fails
        try
        {
            Detail = await _saleManager.GetSaleDetailAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload sale detail for void (SaleID={SaleID})", id);
            return NotFound();
        }

        if (Detail is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            FormatTotals();
            return Page();
        }

        if (Detail.Voided)
        {
            ModelState.AddModelError("", "This sale has already been voided.");
            FormatTotals();
            return Page();
        }

        var staffUserIdRaw = User.FindFirstValue("UserID");
        if (!int.TryParse(staffUserIdRaw, out var staffUserId))
        {
            ModelState.AddModelError("", "Unable to identify the logged-in staff member.");
            FormatTotals();
            return Page();
        }

        try
        {
            var success = await _saleManager.VoidSaleAsync(id, VoidReason!, staffUserId);

            if (success)
            {
                _logger.LogInformation(
                    "Sale voided — SaleID={SaleID} by StaffUserID={StaffUserID} Reason={Reason}",
                    id, staffUserId, VoidReason);

                // PRG: redirect so a browser refresh doesn't re-post the void
                return RedirectToPage("/Sale/Detail", new { id });
            }

            ModelState.AddModelError("", "The sale could not be voided. Please try again.");
        }
        catch (CartValidationException ex)
        {
            // Thrown by SaleManagerService when reason is null/whitespace (belt-and-suspenders)
            _logger.LogWarning(ex, "Void validation error for SaleID={SaleID}", id);
            ModelState.AddModelError(nameof(VoidReason), ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error voiding sale SaleID={SaleID}", id);
            ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
        }

        FormatTotals();
        return Page();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void FormatTotals()
    {
        (TotalUsd, TotalKhr) = CurrencyFormatter.Format(Detail.Total, _paymentOptions.KhrExchangeRate);
    }
}
