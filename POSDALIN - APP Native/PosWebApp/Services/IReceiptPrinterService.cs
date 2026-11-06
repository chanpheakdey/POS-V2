using PosWebApp.Models.DTOs;

namespace PosWebApp.Services;

/// <summary>
/// Receipt_Printer — generates printable receipt content from a <see cref="SaleDetailDto"/>.
///
/// Supports two formats:
/// <list type="bullet">
///   <item><description>HTML receipt — rendered via Razor view, suitable for <c>window.print()</c> and browser print-to-PDF.</description></item>
///   <item><description>Plain-text receipt — 80-column formatted, suitable for thermal printers.</description></item>
/// </list>
///
/// Requirements: 26.1–26.12, 8.10
/// </summary>
public interface IReceiptPrinterService
{
    /// <summary>
    /// Renders an HTML receipt for <paramref name="sale"/> using the Razor view
    /// <c>Shared/_Receipt.cshtml</c>.  The resulting HTML string is embedded in the
    /// page and printed via <c>window.print()</c>.
    ///
    /// Requirements: 26.1, 26.2, 26.3, 26.4, 26.5, 26.6, 26.7, 26.8, 26.9, 26.10
    /// </summary>
    Task<string> GenerateHtmlReceiptAsync(SaleDetailDto sale);

    /// <summary>
    /// Generates a plain-text receipt for 80-column thermal paper.
    ///
    /// Requirements: 26.1–26.12
    /// </summary>
    string GenerateTextReceipt(SaleDetailDto sale);
}
