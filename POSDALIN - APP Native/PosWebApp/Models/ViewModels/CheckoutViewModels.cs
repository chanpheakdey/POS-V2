using System.ComponentModel.DataAnnotations;

namespace PosWebApp.Models.ViewModels;

// ─────────────────────────────────────────────────────────────────────────────
//  Checkout input models
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Input model bound on POST to the Checkout page.
/// Carries payment method selection and the method-specific fields.
/// Requirements: 8.1, 8.2, 27.1–27.9
/// </summary>
public class CheckoutInputModel
{
    /// <summary>
    /// Selected payment method: "Cash", "Card", or "Other".
    /// Requirement 27.1
    /// </summary>
    [Required(ErrorMessage = "Please select a payment method.")]
    public string PaymentMethod { get; set; } = "Cash";

    /// <summary>
    /// Optional customer ID selected via the autocomplete lookup.
    /// Requirement 8.4
    /// </summary>
    public int? CustomerID { get; set; }

    // ── Cash fields ──────────────────────────────────────────────────────────

    /// <summary>
    /// Amount tendered by the customer. Required when PaymentMethod is "Cash".
    /// Requirement 27.4
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount tendered must be greater than zero.")]
    public decimal? AmountTendered { get; set; }

    // ── Card fields ──────────────────────────────────────────────────────────

    /// <summary>
    /// Card network type: "Visa", "Mastercard", or "Other".
    /// Requirement 27.6
    /// </summary>
    public string? CardType { get; set; }

    /// <summary>
    /// Last 4 digits of the payment card.
    /// Requirement 27.6
    /// </summary>
    [StringLength(4, MinimumLength = 4, ErrorMessage = "Last 4 digits must be exactly 4 characters.")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Please enter exactly 4 digits.")]
    public string? CardLastFour { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Checkout page view model (GET)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// View model for the GET handler of the Checkout page.
/// Carries cart summary, formatted totals, and initial form values.
/// Requirements: 8.1, 8.2
/// </summary>
public class CheckoutViewModel
{
    /// <summary>Cart items for the order summary display.</summary>
    public IReadOnlyList<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();

    /// <summary>Grand total formatted as USD string (e.g. "$45.00").</summary>
    public string TotalUsdText { get; set; } = "$0.00";

    /// <summary>Grand total formatted as KHR string (e.g. "184,500៛").</summary>
    public string TotalKhrText { get; set; } = "0៛";

    /// <summary>Raw decimal total for change-calculation logic in the view.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary><see langword="true"/> when the cart is empty (redirect to cart should occur).</summary>
    public bool IsEmpty => !CartItems.Any();
}

// ─────────────────────────────────────────────────────────────────────────────
//  Confirmation page view model
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// View model for the confirmation page after a successful sale.
/// Requirements: 8.7, 8.10, 27.7–27.9, 32.1–32.5
/// </summary>
public class CheckoutConfirmationViewModel
{
    /// <summary>The newly created sale ID.</summary>
    public int SaleID { get; set; }

    /// <summary>The invoice number assigned by the Backend_Service.</summary>
    public string InvoiceNo { get; set; } = "";

    /// <summary>Payment method used: "Cash", "Card", or "Other".</summary>
    public string PaymentMethod { get; set; } = "";

    /// <summary>Grand total formatted as USD string.</summary>
    public string TotalUsdText { get; set; } = "$0.00";

    /// <summary>Grand total formatted as KHR string.</summary>
    public string TotalKhrText { get; set; } = "0៛";

    /// <summary>Raw decimal total.</summary>
    public decimal TotalAmount { get; set; }

    // ── Cash-specific ────────────────────────────────────────────────────────

    /// <summary>Amount tendered (Cash only). Null for non-cash methods.</summary>
    public decimal? AmountTendered { get; set; }

    /// <summary>Pre-formatted change due string (Cash only).</summary>
    public string? ChangeUsdText { get; set; }

    /// <summary>Pre-formatted change due in KHR (Cash only).</summary>
    public string? ChangeKhrText { get; set; }

    // ── Card-specific ────────────────────────────────────────────────────────

    /// <summary>Card network (Card only).</summary>
    public string? CardType { get; set; }

    /// <summary>Last 4 digits of the card (Card only).</summary>
    public string? CardLastFour { get; set; }

    // ── Customer ─────────────────────────────────────────────────────────────

    /// <summary>Customer name if a customer was linked to the sale.</summary>
    public string? CustomerName { get; set; }

    // ── Cart items for receipt printing ──────────────────────────────────────

    /// <summary>Cart items snapshot for the receipt.</summary>
    public IReadOnlyList<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
}

// ─────────────────────────────────────────────────────────────────────────────
//  TempData carrier for post–redirect–get pattern
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Snapshot stored in TempData after a successful sale so the confirmation page
/// can display order details without querying the backend again.
/// Requirement 32.1
/// </summary>
public record CheckoutTempData(
    int SaleID,
    string InvoiceNo,
    string PaymentMethod,
    decimal TotalAmount,
    decimal? AmountTendered,
    string? CardType,
    string? CardLastFour,
    string? CustomerName,
    IReadOnlyList<CartItemViewModel> CartItems);
