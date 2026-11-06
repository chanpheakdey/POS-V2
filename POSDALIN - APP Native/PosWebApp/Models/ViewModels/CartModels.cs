namespace PosWebApp.Models.ViewModels;

// ─────────────────────────────────────────────────────────────────────────────
//  Session models (stored as JSON in ASP.NET Core session state)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A single item held in the session cart.
/// LineTotal accounts for an optional percentage discount.
/// </summary>
public record CartItem(
    int ItemId,
    string ItemCode,
    string ItemName,
    string ImageUrl,
    int Quantity,
    decimal UnitPrice,
    int PriceTierId,
    decimal Discount = 0m,
    string DiscountType = "Percentage",
    int AuthorizingUserId = 0)
{
    /// <summary>
    /// Line total after discount: UnitPrice × (1 − Discount/100) × Quantity.
    /// </summary>
    public decimal LineTotal => (UnitPrice * (1 - Discount / 100)) * Quantity;

    /// <summary>Original line total before any discount (UnitPrice × Quantity).</summary>
    public decimal OriginalLineTotal => UnitPrice * Quantity;

    /// <summary>True when a discount greater than zero has been applied.</summary>
    public bool HasDiscount => Discount > 0m;
}

/// <summary>
/// The full cart stored in session state. Not persisted to the database.
/// </summary>
public record CartState(List<CartItem> Items)
{
    /// <summary>Sum of all item line totals.</summary>
    public decimal Total => Items.Sum(i => i.LineTotal);

    /// <summary>Total number of individual units across all cart entries.</summary>
    public int ItemCount => Items.Sum(i => i.Quantity);

    /// <summary>Returns a new, empty <see cref="CartState"/>.</summary>
    public static CartState Empty => new CartState(new List<CartItem>());
}

// ─────────────────────────────────────────────────────────────────────────────
//  View models (used only in Razor Pages — not persisted to session)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// View model for a single row in the cart table.
/// Contains pre-formatted USD strings so the Razor view is free of formatting logic.
/// </summary>
public class CartItemViewModel
{
    /// <summary>Primary key of the item.</summary>
    public int ItemId { get; set; }

    /// <summary>Short alphanumeric item code displayed in the table (Req 17.3).</summary>
    public string ItemCode { get; set; } = "";

    /// <summary>Full item display name (Req 17.3).</summary>
    public string ItemName { get; set; } = "";

    /// <summary>Number of units in the cart (Req 17.3).</summary>
    public int Quantity { get; set; }

    /// <summary>Unit price formatted as a USD string, e.g. <c>$12.50</c> (Req 17.3).</summary>
    public string UnitPriceUsd { get; set; } = "";

    /// <summary>Line total (unit price × qty) formatted as a USD string (Req 17.3).</summary>
    public string LineTotalUsd { get; set; } = "";

    // ── Discount display (Requirements 28.5, 28.6) ─────────────────────────

    /// <summary>Applied discount percentage (0 = no discount).</summary>
    public decimal Discount { get; set; }

    /// <summary>True when a non-zero discount is applied.</summary>
    public bool HasDiscount => Discount > 0m;

    /// <summary>Original line total before discount, formatted as USD string.</summary>
    public string OriginalLineTotalUsd { get; set; } = "";
}

/// <summary>
/// Page view model for <c>Cart.cshtml</c>.
/// Carries the list of cart item rows and pre-formatted grand totals (Req 17.7).
/// </summary>
public class CartViewModel
{
    /// <summary>Ordered list of cart item rows.</summary>
    public IReadOnlyList<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();

    /// <summary>Grand total formatted as a USD string, e.g. <c>$45.00</c>.</summary>
    public string TotalUsdText { get; set; } = "$0.00";

    /// <summary>Grand total formatted as a KHR string, e.g. <c>184,500៛</c>.</summary>
    public string TotalKhrText { get; set; } = "0៛";

    /// <summary><see langword="true"/> when the cart contains at least one item (Req 17.10).</summary>
    public bool HasItems => Items.Any();
}

/// <summary>
/// Input model for the apply-discount AJAX handler (Requirements 28.1–28.5).
/// </summary>
public class ApplyDiscountRequest
{
    /// <summary>Cart item to discount.</summary>
    public int ItemId { get; set; }

    /// <summary>Discount percentage, 0–100.</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>"Percentage" or "Fixed".</summary>
    public string DiscountType { get; set; } = "Percentage";

    /// <summary>UserID of the authorising manager/admin (required for discounts > 20%).</summary>
    public int AuthorizingUserId { get; set; }

    /// <summary>PIN/password supplied for high-value (> 20%) discount authorisation.</summary>
    public string? AuthorizingPin { get; set; }
}
