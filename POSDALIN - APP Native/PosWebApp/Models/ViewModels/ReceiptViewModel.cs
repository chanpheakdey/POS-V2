namespace PosWebApp.Models.ViewModels;

/// <summary>
/// View model passed to <c>Shared/_Receipt.cshtml</c> during HTML receipt generation.
/// Requirements: 26.1–26.12
/// </summary>
public class ReceiptViewModel
{
    // ── Business info (from appsettings.json Receipt section) ─────────────────

    /// <summary>Business name shown at the top of the receipt.</summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>Business address.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Business phone number.</summary>
    public string Phone { get; set; } = string.Empty;

    // ── Sale header ────────────────────────────────────────────────────────────

    /// <summary>Unique sale identifier.</summary>
    public int SaleID { get; set; }

    /// <summary>Human-readable invoice number.</summary>
    public string InvoiceNo { get; set; } = string.Empty;

    /// <summary>Date and time the sale was created.</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>Name of the staff member who processed the sale.</summary>
    public string StaffUserName { get; set; } = string.Empty;

    /// <summary>Customer name, if a customer was linked to the sale.</summary>
    public string? CustomerName { get; set; }

    // ── Line items ─────────────────────────────────────────────────────────────

    /// <summary>Individual line items for this sale.</summary>
    public IReadOnlyList<ReceiptLineItem> Items { get; set; } = Array.Empty<ReceiptLineItem>();

    // ── Totals ─────────────────────────────────────────────────────────────────

    /// <summary>Subtotal (before any fees/taxes — same as grand total in this system).</summary>
    public double Subtotal { get; set; }

    /// <summary>Grand total in USD as a pre-formatted string, e.g. "$45.00".</summary>
    public string TotalUsd { get; set; } = "$0.00";

    /// <summary>Grand total in KHR as a pre-formatted string, e.g. "184,500៛".</summary>
    public string TotalKhr { get; set; } = "0៛";
}

/// <summary>
/// A single line item within a receipt.
/// </summary>
public class ReceiptLineItem
{
    /// <summary>Product name.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Quantity sold.</summary>
    public int Quantity { get; set; }

    /// <summary>Unit price in USD.</summary>
    public double UnitPrice { get; set; }

    /// <summary>Line total (Quantity × UnitPrice − Discount) in USD.</summary>
    public double LineTotal { get; set; }

    /// <summary>Applied discount percentage (0 = none).</summary>
    public double Discount { get; set; }

    /// <summary>Original line total before discount (Quantity × UnitPrice).</summary>
    public double OriginalLineTotal => UnitPrice * Quantity;

    /// <summary>True when a non-zero discount was applied.</summary>
    public bool HasDiscount => Discount > 0d;
}
