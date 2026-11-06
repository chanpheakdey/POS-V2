using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using PosWebApp.Infrastructure;
using PosWebApp.Infrastructure.Exceptions;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Models.DTOs;
using PosWebApp.Models.ViewModels;
using PosWebApp.Resources;
using PosWebApp.Services;
using System.Security.Claims;
using System.Text.Json;

namespace PosWebApp.Pages;

/// <summary>
/// Checkout page — collects payment method (Cash/Card/Other), optional customer
/// lookup, then calls <see cref="ISaleManagerService.ProcessSaleAsync"/> to submit
/// the sale to the Backend_Service.
///
/// On success, stores a <see cref="CheckoutTempData"/> snapshot in TempData and
/// redirects to the Confirmation handler.
///
/// On failure (<see cref="CartValidationException"/> or <see cref="SaleProcessingException"/>),
/// re-renders the form with a <c>ModelState</c> error.
///
/// Requirements: 8.1, 8.2, 8.4, 8.7, 8.10, 27.1, 27.2, 27.3, 27.4, 27.5,
///               27.6, 27.7, 27.8, 27.9, 32.1, 32.2, 32.3, 32.4, 32.5
/// </summary>
[Authorize]
[ValidateAntiForgeryToken]
public class CheckoutModel : PageModel
{
    private readonly ICartService _cart;
    private readonly ISaleManagerService _saleManager;
    private readonly IServiceClient _serviceClient;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<CheckoutModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    // TempData key used to carry the sale snapshot across the PRG redirect.
    private const string TempDataKey = "CheckoutResult";

    public CheckoutModel(
        ICartService cart,
        ISaleManagerService saleManager,
        IServiceClient serviceClient,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<CheckoutModel> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _cart           = cart;
        _saleManager    = saleManager;
        _serviceClient  = serviceClient;
        _paymentOptions = paymentOptions.Value;
        _logger         = logger;
        _localizer      = localizer;
    }

    // ── Page properties ──────────────────────────────────────────────────────

    /// <summary>View model built from the current session cart (GET).</summary>
    public CheckoutViewModel ViewModel { get; private set; } = new();

    /// <summary>Confirmation view model (Confirmation handler).</summary>
    public CheckoutConfirmationViewModel ConfirmationViewModel { get; private set; } = new();

    /// <summary>Form input bound on POST.</summary>
    [BindProperty]
    public CheckoutInputModel Input { get; set; } = new();

    /// <summary>Exposes the KHR exchange rate to the view for client-side change calculation.</summary>
    public double KhrExchangeRate => _paymentOptions.KhrExchangeRate;

    // ── GET ───────────────────────────────────────� �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � � �\ � */
    /// Redirects to <c>/Cart</c> when the cart is empty.
    ///
    /// Requirements: 8.1, 8.2
    /// </summary>
    public Task<IActionResult> OnGetAsync()
    {
        BuildViewModel();

        if (ViewModel.IsEmpty)
        {
            _logger.LogInformation(
                "Checkout.OnGetAsync — cart empty, redirecting to /Cart");
            return Task.FromResult<IActionResult>(RedirectToPage("/Cart"));
        }

        return Task.FromResult<IActionResult>(Page());
    }

    // ── POST (main checkout submit) ──────────────────────────────────────────

    /// <summary>
    /// Validates input, applies payment-method-specific validation, then calls
    /// <see cref="ISaleManagerService.ProcessSaleAsync"/>.
    ///
    /// On success: stores cart snapshot in TempData and redirects to the
    /// Confirmation handler.
    ///
    /// On failure: re-renders the form with ModelState errors.
    ///
    /// Requirements: 8.2, 8.4, 8.7, 27.1–27.6
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        // Rebuild cart view model so the form can be re-rendered on error.
        BuildViewModel();

        ApplyPaymentMethodValidation();

        if (!ModelState.IsValid)
            return Page();

        var staffUserId = GetStaffUserId();
        if (staffUserId == 0)
        {
            ModelState.AddModelError("", "Unable to identify the logged-in staff member.");
            return Page();
        }

        var request = new ProcessSaleRequest(
            Session:       HttpContext.Session,
            StaffUserID:   staffUserId,
            CustomerID:    Input.CustomerID,
            PaymentMethod: Input.PaymentMethod);

        try
        {
            var result = await _saleManager.ProcessSaleAsync(request);

            _logger.LogInformation(
                "Sale processed: SaleID={SaleID} InvoiceNo={InvoiceNo} PaymentMethod={PaymentMethod} StaffID={StaffID}",
                result.SaleID, result.InvoiceNo, Input.PaymentMethod, staffUserId);

            // Build cart item snapshot BEFORE clearing; ProcessSaleAsync clears the
            // cart internally, so we must capture it from ViewModel (already built above).
            StoreTempData(result, ViewModel);

            return RedirectToPage("/Checkout", "Confirmation", new { saleId = result.SaleID });
        }
        catch (CartValidationException ex)
        {
            _logger.LogWarning(ex, "Cart validation failed during checkout");
            ModelState.AddModelError("", ex.Message);
            BuildViewModel(); // Cart may now be empty if it was cleared by another tab.
            return Page();
        }
        catch (SaleProcessingException ex)
        {
            _logger.LogError(ex, "Sale processing failed");
            ModelState.AddModelError("", "Sale could not be processed: " + ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during checkout");
            ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
            return Page();
        }
    }

    // ── GET (Confirmation handler) ───────────────────────────────────────────

    /// <summary>
    /// Displays the sale confirmation page using the TempData snapshot stored
    /// by <see cref="OnPostAsync"/>.
    ///
    /// If TempData is missing (direct navigation / page refresh), redirects to
    /// the Sale detail page when a <paramref name="saleId"/> is present, or to
    /// the home page otherwise.
    ///
    /// Requirements: 8.7, 8.10, 27.7–27.9, 32.1–32.5
    /// </summary>
    public IActionResult OnGetConfirmation(int? saleId)
    {
        // Attempt to read the snapshot from TempData.
        var json = TempData[TempDataKey] as string;

        if (json is null)
        {
            _logger.LogWarning(
                "Checkout.OnGetConfirmation — TempData missing (saleId={SaleId})", saleId);
            // If we have a saleId, send the user to the sale detail page instead.
            if (saleId.HasValue)
                return RedirectToPage("/Sale/Detail", new { id = saleId.Value });
            return RedirectToPage("/Index");
        }

        CheckoutTempData? data = null;
        try
        {
            data = JsonSerializer.Deserialize<CheckoutTempData>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize CheckoutTempData from TempData");
        }

        if (data is null)
            return RedirectToPage("/Index");

        ConfirmationViewModel = BuildConfirmationViewModel(data);
        return Page();
    }

    // ── AJAX: Customer search ────────────────────────────────────────────────

    /// <summary>
    /// Handles AJAX GET requests from the customer-lookup autocomplete field.
    /// Called as <c>GET /Checkout?handler=SearchCustomers&amp;q={query}</c>.
    ///
    /// Returns a JSON array of <c>{ id, name, phone }</c> objects.
    ///
    /// Requirement 8.4
    /// </summary>
    public async Task<IActionResult> OnGetSearchCustomersAsync(string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return new JsonResult(Array.Empty<object>());

        try
        {
            var customers = await _serviceClient.SearchCustomersAsync(q);
            var results   = customers.Select(c => new
            {
                id    = c.CustomerID,
                name  = c.CustomerName,
                phone = c.Phone,
            });
            return new JsonResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer search failed for query={Query}", q);
            return new JsonResult(Array.Empty<object>());
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds <see cref="ViewModel"/> from the current session cart state.
    /// </summary>
    private void BuildViewModel()
    {
        var state = _cart.GetCart(HttpContext.Session);
        var rate  = _paymentOptions.KhrExchangeRate;

        var items = state.Items.Select(ci =>
        {
            var (lineTotalUsd, _) = CurrencyFormatter.Format(ci.LineTotal, rate);
            var (unitPriceUsd, _) = CurrencyFormatter.Format(ci.UnitPrice, rate);
            return new CartItemViewModel
            {
                ItemId       = ci.ItemId,
                ItemCode     = ci.ItemCode,
                ItemName     = ci.ItemName,
                Quantity     = ci.Quantity,
                UnitPriceUsd = unitPriceUsd,
                LineTotalUsd = lineTotalUsd,
            };
        }).ToList();

        var (totalUsd, totalKhr) = CurrencyFormatter.Format(state.Total, rate);

        ViewModel = new CheckoutViewModel
        {
            CartItems    = items,
            TotalUsdText = totalUsd,
            TotalKhrText = totalKhr,
            TotalAmount  = state.Total,
        };
    }

    /// <summary>
    /// Applies extra validation rules that depend on the selected payment method
    /// (Cash requires AmountTendered ≥ Total; Card requires CardType and CardLastFour).
    /// Requirements: 27.3, 27.4, 27.5, 27.6
    /// </summary>
    private void ApplyPaymentMethodValidation()
    {
        var total = _cart.CalculateTotal(HttpContext.Session);

        if (Input.PaymentMethod == "Cash")
        {
            if (Input.AmountTendered is null or <= 0d)
            {
                ModelState.AddModelError(
                    nameof(Input.AmountTendered),
                    "Please enter the amount tendered.");
            }
            else if (Input.AmountTendered < total)
            {
                ModelState.AddModelError(
                    nameof(Input.AmountTendered),
                    $"Amount tendered (${Input.AmountTendered:F2}) must be at least the order total (${total:F2}).");
            }
        }
        else if (Input.PaymentMethod == "Card")
        {
            if (string.IsNullOrWhiteSpace(Input.CardType))
            {
                ModelState.AddModelError(nameof(Input.CardType), "Please select a card type.");
            }

            if (string.IsNullOrWhiteSpace(Input.CardLastFour))
            {
                ModelState.AddModelError(
                    nameof(Input.CardLastFour),
                    "Please enter the last 4 digits of the card.");
            }
        }
    }

    /// <summary>Reads the staff UserID claim from the current user principal.</summary>
    private int GetStaffUserId()
    {
        var raw = User.FindFirstValue("UserID");
        return int.TryParse(raw, out var id) ? id : 0;
    }

    /// <summary>
    /// Serializes the sale result plus the cart snapshot into TempData for the
    /// Post-Redirect-Get confirmation page.
    /// Requirement 32.1
    /// </summary>
    private void StoreTempData(SaleResult result, CheckoutViewModel snapshot)
    {
        var data = new CheckoutTempData(
            SaleID:        result.SaleID,
            InvoiceNo:     result.InvoiceNo,
            PaymentMethod: Input.PaymentMethod,
            TotalAmount:   snapshot.TotalAmount,
            AmountTendered: Input.PaymentMethod == "Cash" ? Input.AmountTendered : null,
            CardType:       Input.PaymentMethod == "Card" ? Input.CardType : null,
            CardLastFour:   Input.PaymentMethod == "Card" ? Input.CardLastFour : null,
            CustomerName:   null, // resolved by name if needed — not available at this point without extra lookup
            CartItems:      snapshot.CartItems);

        TempData[TempDataKey] = JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// Maps a <see cref="CheckoutTempData"/> snapshot to the
    /// <see cref="CheckoutConfirmationViewModel"/> presented on the confirmation page.
    /// Requirements: 27.7–27.9, 32.2–32.5
    /// </summary>
    private CheckoutConfirmationViewModel BuildConfirmationViewModel(CheckoutTempData data)
    {
        var rate = _paymentOptions.KhrExchangeRate;
        var (totalUsd, totalKhr) = CurrencyFormatter.Format(data.TotalAmount, rate);

        string? changeUsd = null;
        string? changeKhr = null;
        if (data.PaymentMethod == "Cash" && data.AmountTendered.HasValue)
        {
            var change = data.AmountTendered.Value - data.TotalAmount;
            (changeUsd, changeKhr) = CurrencyFormatter.Format(change, rate);
        }

        return new CheckoutConfirmationViewModel
        {
            SaleID         = data.SaleID,
            InvoiceNo      = data.InvoiceNo,
            PaymentMethod  = data.PaymentMethod,
            TotalUsdText   = totalUsd,
            TotalKhrText   = totalKhr,
            TotalAmount    = data.TotalAmount,
            AmountTendered = data.AmountTendered,
            ChangeUsdText  = changeUsd,
            ChangeKhrText  = changeKhr,
            CardType       = data.CardType,
            CardLastFour   = data.CardLastFour,
            CustomerName   = data.CustomerName,
            CartItems      = data.CartItems,
        };
    }
}
