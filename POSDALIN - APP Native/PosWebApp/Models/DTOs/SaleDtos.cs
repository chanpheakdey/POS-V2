namespace PosWebApp.Models.DTOs;

/// <summary>Returned by createSale endpoint on success.</summary>
public record SaleCreationResult(int SaleID, string InvoiceNo);

/// <summary>Single row in the sale history list from getSaleHistory endpoint.</summary>
public record SaleListItemDto(
    int SaleID,
    string InvoiceNo,
    DateTime CreatedDate,
    string StaffUserName,
    string? CustomerName,
    double Total,
    bool Voided);

/// <summary>Full sale detail including line items, returned by getSaleDetail endpoint.</summary>
public record SaleDetailDto(
    int SaleID,
    string InvoiceNo,
    DateTime CreatedDate,
    string StaffUserName,
    string? CustomerName,
    double Total,
    string PaymentMethod,
    IReadOnlyList<SaleItemDto> Items,
    bool Voided = false,
    string? VoidedBy = null,
    DateTime? VoidedDate = null,
    string? VoidReason = null);

/// <summary>Single line item within a <see cref="SaleDetailDto"/>.</summary>
public record SaleItemDto(
    int SaleItemID,
    int ItemID,
    string ItemCode,
    string ItemName,
    int Quantity,
    double UnitPrice,
    double Discount,
    double LineTotal);

/// <summary>Customer lookup result returned by SearchCustomers endpoint.</summary>
public record CustomerDto(int CustomerID, string CustomerName, string? Phone);

// ── Request objects ───────────────────────────────────────────────────────────

/// <summary>Payload sent to the createSale ASMX endpoint.</summary>
public record CreateSaleRequest(
    int StaffUserID,
    int? CustomerID,
    string PaymentMethod,
    IReadOnlyList<CreateSaleItemRequest> Items);

/// <summary>Single line-item within a <see cref="CreateSaleRequest"/>.</summary>
public record CreateSaleItemRequest(
    int ItemID,
    int Quantity,
    double UnitPrice,
    double Discount);

// ── Pagination wrapper ────────────────────────────────────────────────────────

/// <summary>Generic paged result returned by list endpoints.</summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// Query parameters used when calling the getSaleHistory endpoint.
/// StaffUserID is null for Manager/Admin (returns all sales); set for Staff (filtered to own sales).
/// </summary>
public record SaleHistoryQuery(
    int? StaffUserID,
    string UserLevel,
    DateTime? StartDate,
    DateTime? EndDate,
    int Page,
    int PageSize);

// ── Sale Manager service types ────────────────────────────────────────────────

/// <summary>
/// Input to <see cref="PosWebApp.Services.ISaleManagerService.ProcessSaleAsync"/>.
/// Carries everything the Sale_Manager needs to build a <see cref="CreateSaleRequest"/>
/// and clear the cart after a successful submission.
/// </summary>
public record ProcessSaleRequest(
    ISession Session,
    int StaffUserID,
    int? CustomerID,
    string PaymentMethod);

/// <summary>
/// Result returned by a successful <see cref="PosWebApp.Services.ISaleManagerService.ProcessSaleAsync"/> call.
/// Mirrors the <see cref="SaleCreationResult"/> from the Backend_Service, surfaced as a
/// first-class Sale_Manager type so callers (Checkout page) remain decoupled from the
/// raw service-client DTO.
/// </summary>
public record SaleResult(int SaleID, string InvoiceNo);
