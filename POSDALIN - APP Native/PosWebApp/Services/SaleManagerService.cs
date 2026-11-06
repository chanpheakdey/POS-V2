using PosWebApp.Infrastructure.Exceptions;
using PosWebApp.Models.DTOs;
using PosWebApp.Models.ViewModels;

namespace PosWebApp.Services;

/// <summary>
/// Implements <see cref="ISaleManagerService"/> by delegating every operation to
/// <see cref="IServiceClient"/>.  No direct database access, no EF Core.
/// </summary>
public sealed class SaleManagerService : ISaleManagerService
{
    private readonly IServiceClient _serviceClient;
    private readonly ICartService   _cartService;
    private readonly ILogger<SaleManagerService> _logger;

    public SaleManagerService(
        IServiceClient serviceClient,
        ICartService   cartService,
        ILogger<SaleManagerService> logger)
    {
        _serviceClient = serviceClient;
        _cartService   = cartService;
        _logger        = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ProcessSaleAsync — Requirement 8.1, 8.2, 8.3, 8.5, 8.6
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<SaleResult> ProcessSaleAsync(ProcessSaleRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        // Validate: cart must not be empty (Requirement 8.1).
        var cart = _cartService.GetCart(request.Session);
        if (!cart.Items.Any())
            throw new CartValidationException("Cart is empty. Please add items before checking out.");

        // Build the ASMX request from the current cart state.
        var lineItems = cart.Items
            .Select(i => new CreateSaleItemRequest(
                ItemID:    i.ItemId,
                Quantity:  i.Quantity,
                UnitPrice: i.UnitPrice,
                Discount:  i.Discount))
            .ToList();

        var createRequest = new CreateSaleRequest(
            StaffUserID:   request.StaffUserID,
            CustomerID:    request.CustomerID,
            PaymentMethod: request.PaymentMethod,
            Items:         lineItems);

        // Submit to Backend_Service (Requirement 8.2, 8.3).
        // IServiceClient wraps failures in SaleProcessingException.
        var result = await _serviceClient.CreateSaleAsync(createRequest);

        _logger.LogInformation(
            "Sale created — SaleID: {SaleID}, InvoiceNo: {InvoiceNo}, StaffUserID: {StaffUserID}",
            result.SaleID, result.InvoiceNo, request.StaffUserID);

        // Clear cart only after a confirmed success (Requirement 8.5).
        // We derive a stable userId string for SignalR from the StaffUserID.
        await _cartService.ClearCartAsync(request.Session, request.StaffUserID.ToString());

        return new SaleResult(result.SaleID, result.InvoiceNo);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  GetSaleHistoryAsync — Requirement 9.1, 9.2, 9.3
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<PagedResult<SaleListItemDto>> GetSaleHistoryAsync(SaleHistoryQuery query)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        // Role-based filtering (Requirements 9.1, 9.2, 9.3):
        //   Staff   → StaffUserID is kept in the query so Backend_Service returns only their sales.
        //   Manager / Admin → StaffUserID must be null so Backend_Service returns all sales.
        var filteredQuery = string.Equals(query.UserLevel, "Staff", StringComparison.OrdinalIgnoreCase)
            ? query                              // Staff: leave StaffUserID as-is (set by caller)
            : query with { StaffUserID = null }; // Manager/Admin: clear any StaffUserID

        return _serviceClient.GetSaleHistoryAsync(filteredQuery);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  GetSaleDetailAsync — Requirement 9.5
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<SaleDetailDto> GetSaleDetailAsync(int saleId)
    {
        return _serviceClient.GetSaleDetailAsync(saleId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  VoidSaleAsync — Requirement 29.3, 29.4, 29.5
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<bool> VoidSaleAsync(int saleId, string reason, int staffUserId)
    {
        // Validate reason before touching the Backend_Service (Requirement 29.9).
        if (string.IsNullOrWhiteSpace(reason))
            throw new CartValidationException("A reason must be provided when voiding a sale.");

        return _serviceClient.VoidSaleAsync(saleId, reason, staffUserId);
    }
}
