using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PosWebApp.Infrastructure;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Models.DTOs;
using PosWebApp.Services;
using System.Security.Claims;

namespace PosWebApp.Pages.Reports;

/// <summary>
/// End-of-Day report page for Managers and Admins.
/// Requirements: 31.1–31.13
/// </summary>
[Authorize(Roles = "Manager,Admin")]
public class EndOfDayModel : PageModel
{
    private readonly ISaleManagerService _saleManager;
    private readonly PaymentOptions _paymentOptions;
    private readonly ILogger<EndOfDayModel> _logger;

    private const int ReportPageSize = 5000;

    public EndOfDayModel(
        ISaleManagerService saleManager,
        IOptions<PaymentOptions> paymentOptions,
        ILogger<EndOfDayModel> logger)
    {
        _saleManager = saleManager;
        _paymentOptions = paymentOptions.Value;
        _logger = logger;
    }

    // ── Bound query parameters ────────────────────────────────────────────────

    /// <summary>Report start date (inclusive). Defaults to today 00:00.</summary>
    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; }

    /// <summary>Report end date (inclusive, time set to 23:59:59). Defaults to today.</summary>
    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; }

    // ── Report summary properties ─────────────────────────────────────────────

    /// <summary>All sales returned by the service for this date range.</summary>
    public IReadOnlyList<SaleListItemDto> AllSales { get; private set; } =
        Array.Empty<SaleListItemDto>();

    /// <summary>Count of non-voided sales. Requirement 31.3</summary>
    public int TotalSalesCount { get; private set; }

    /// <summary>Sum of Total for non-voided sales. Requirement 31.4</summary>
    public decimal TotalRevenue { get; private set; }

    /// <summary>Formatted total revenue USD. Requirement 31.5</summary>
    public string TotalRevenueUsd { get; private set; } = "$0.00";

    /// <summary>Formatted total revenue KHR. Requirement 31.5</summary>
    public string TotalRevenueKhr { get; private set; } = "0";

    /// <summary>Count of voided sales. Requirement 31.6</summary>
    public int VoidedSalesCount { get; private set; }

    /// <summary>Breakdown by staff member. Requirement 31.7</summary>
    public IReadOnlyList<StaffBreakdownRow> StaffBreakdown { get; private set; } =
        Array.Empty<StaffBreakdownRow>();

    /// <summary>Whether data has been loaded successfully.</summary>
    public bool HasData { get; private set; }

    /// <summary>Error message displayed when the service call fails.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// KHR exchange rate exposed to the Razor view for per-row currency formatting.
    /// </summary>
    public decimal ExchangeRate => _paymentOptions.KhrExchangeRate;

    // ── GET handler ───────────────────────────────────────────────────────────

    /// <summary>
    /// Loads all sales for the selected date range and aggregates the data client-side.
    /// Defaults to today when no dates are supplied.
    /// Requirements: 31.1, 31.2, 31.3, 31.4, 31.5, 31.6, 31.7
    /// </summary>
    public async Task OnGetAsync()
    {
        // Default date range: today 00:00 – 23:59:59
        var today = DateTime.Today;
        if (StartDate == default) StartDate = today;
        if (EndDate == default)   EndDate   = today;

        var endDateInclusive = EndDate.Date.AddDays(1).AddSeconds(-1);

        var userLevel = User.FindFirstValue("UserLevel") ?? "Manager";

        var query = new SaleHistoryQuery(
            StaffUserID: null,  // Manager/Admin — no staff filter
            UserLevel:   userLevel,
            StartDate:   StartDate.Date,
            EndDate:     endDateInclusive,
            Page:        1,
            PageSize:    ReportPageSize);

        try
        {
            var result = await _saleManager.GetSaleHistoryAsync(query);
            AllSales = result.Items;
            BuildAggregates();
            HasData = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load End-of-Day report (Start={Start}, End={End})",
                StartDate, EndDate);
            ErrorMessage = "Could not load report data. Please try again.";
        }
    }

    // ── Excel export handler — Requirement 31.11 ──────────────────────────────

    /// <summary>
    /// Exports the End-of-Day report to an Excel workbook using ClosedXML.
    /// Requirements: 31.11, 31.12
    /// </summary>
    public async Task<IActionResult> OnGetExportExcelAsync(
        DateTime? startDate, DateTime? endDate)
    {
        var today = DateTime.Today;
        var start = (startDate ?? today).Date;
        var end   = (endDate ?? today).Date.AddDays(1).AddSeconds(-1);

        var userLevel = User.FindFirstValue("UserLevel") ?? "Manager";

        var query = new SaleHistoryQuery(
            StaffUserID: null,
            UserLevel:   userLevel,
            StartDate:   start,
            EndDate:     end,
            Page:        1,
            PageSize:    ReportPageSize);

        IReadOnlyList<SaleListItemDto> sales;
        try
        {
            var result = await _saleManager.GetSaleHistoryAsync(query);
            sales = result.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel export failed");
            return BadRequest("Could not retrieve data for export.");
        }

        var stream = BuildExcelWorkbook(sales, start, end);
        var fileName = $"EndOfDay_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx";
        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds all aggregated report sections from <see cref="AllSales"/>.
    /// All aggregation is done in C# LINQ — no EF queries. Requirement 31.2
    /// </summary>
    private void BuildAggregates()
    {
        var active  = AllSales.Where(s => !s.Voided).ToList();
        var voided  = AllSales.Where(s => s.Voided).ToList();

        // Total sales count and revenue (non-voided) — Requirements 31.3, 31.4
        TotalSalesCount = active.Count;
        TotalRevenue    = active.Sum(s => s.Total);
        VoidedSalesCount = voided.Count;

        // Format revenue — Requirement 31.5
        (TotalRevenueUsd, TotalRevenueKhr) =
            CurrencyFormatter.Format(TotalRevenue, _paymentOptions.KhrExchangeRate);

        // Breakdown by staff — Requirement 31.7
        StaffBreakdown = active
            .GroupBy(s => s.StaffUserName)
            .Select(g =>
            {
                var rev = g.Sum(s => s.Total);
                var (usd, khr) = CurrencyFormatter.Format(rev, _paymentOptions.KhrExchangeRate);
                return new StaffBreakdownRow(
                    StaffUserName: g.Key,
                    SaleCount:     g.Count(),
                    Revenue:       rev,
                    RevenueUsd:    usd,
                    RevenueKhr:    khr);
            })
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }

    /// <summary>
    /// Builds the Excel workbook for the End-of-Day report using ClosedXML.
    /// Returns the workbook as a MemoryStream. Requirement 31.11, 31.12
    /// </summary>
    private MemoryStream BuildExcelWorkbook(
        IReadOnlyList<SaleListItemDto> sales, DateTime start, DateTime end)
    {
        var wb = new XLWorkbook();

        // ── Sheet 1: Summary ──────────────────────────────────────────────
        var summary = wb.Worksheets.Add("Summary");

        summary.Cell(1, 1).Value = "End-of-Day Report";
        summary.Cell(1, 1).Style.Font.Bold = true;
        summary.Cell(1, 1).Style.Font.FontSize = 14;

        summary.Cell(2, 1).Value = $"Period: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}";
        summary.Cell(3, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

        summary.Cell(5, 1).Value = "Metric";
        summary.Cell(5, 2).Value = "Value";
        summary.Row(5).Style.Font.Bold = true;

        var active  = sales.Where(s => !s.Voided).ToList();
        var voided  = sales.Where(s => s.Voided).ToList();
        var revenue = active.Sum(s => s.Total);

        summary.Cell(6, 1).Value = "Total Sales (non-voided)";
        summary.Cell(6, 2).Value = active.Count;

        summary.Cell(7, 1).Value = "Total Revenue (USD)";
        summary.Cell(7, 2).Value = revenue;
        summary.Cell(7, 2).Style.NumberFormat.Format = "$#,##0.00";

        summary.Cell(8, 1).Value = "Voided Sales";
        summary.Cell(8, 2).Value = voided.Count;

        summary.Columns().AdjustToContents();

        // ── Sheet 2: Staff Breakdown ──────────────────────────────────────
        var staffSheet = wb.Worksheets.Add("Staff Breakdown");

        staffSheet.Cell(1, 1).Value = "Staff";
        staffSheet.Cell(1, 2).Value = "Sales Count";
        staffSheet.Cell(1, 3).Value = "Revenue (USD)";
        staffSheet.Row(1).Style.Font.Bold = true;

        var staffRows = active
            .GroupBy(s => s.StaffUserName)
            .Select(g => (Name: g.Key, Count: g.Count(), Rev: g.Sum(s => s.Total)))
            .OrderByDescending(r => r.Rev)
            .ToList();

        for (int i = 0; i < staffRows.Count; i++)
        {
            var row = staffRows[i];
            staffSheet.Cell(i + 2, 1).Value = row.Name;
            staffSheet.Cell(i + 2, 2).Value = row.Count;
            staffSheet.Cell(i + 2, 3).Value = row.Rev;
            staffSheet.Cell(i + 2, 3).Style.NumberFormat.Format = "$#,##0.00";
        }

        staffSheet.Columns().AdjustToContents();

        // ── Sheet 3: All Sales ────────────────────────────────────────────
        var salesSheet = wb.Worksheets.Add("All Sales");

        salesSheet.Cell(1, 1).Value = "Sale ID";
        salesSheet.Cell(1, 2).Value = "Invoice No.";
        salesSheet.Cell(1, 3).Value = "Date";
        salesSheet.Cell(1, 4).Value = "Staff";
        salesSheet.Cell(1, 5).Value = "Customer";
        salesSheet.Cell(1, 6).Value = "Total (USD)";
        salesSheet.Cell(1, 7).Value = "Voided";
        salesSheet.Row(1).Style.Font.Bold = true;

        for (int i = 0; i < sales.Count; i++)
        {
            var s = sales[i];
            salesSheet.Cell(i + 2, 1).Value = s.SaleID;
            salesSheet.Cell(i + 2, 2).Value = s.InvoiceNo;
            salesSheet.Cell(i + 2, 3).Value = s.CreatedDate;
            salesSheet.Cell(i + 2, 3).Style.NumberFormat.Format = "yyyy-mm-dd hh:mm";
            salesSheet.Cell(i + 2, 4).Value = s.StaffUserName;
            salesSheet.Cell(i + 2, 5).Value = s.CustomerName ?? "";
            salesSheet.Cell(i + 2, 6).Value = s.Total;
            salesSheet.Cell(i + 2, 6).Style.NumberFormat.Format = "$#,##0.00";
            salesSheet.Cell(i + 2, 7).Value = s.Voided ? "Yes" : "No";
        }

        salesSheet.Columns().AdjustToContents();

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}

// ── Supporting records ────────────────────────────────────────────────────────

/// <summary>One row in the staff breakdown section. Requirement 31.7</summary>
public record StaffBreakdownRow(
    string StaffUserName,
    int    SaleCount,
    decimal Revenue,
    string RevenueUsd,
    string RevenueKhr);
