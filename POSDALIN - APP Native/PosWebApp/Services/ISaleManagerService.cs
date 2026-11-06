using PosWebApp.Models.DTOs;

namespace PosWebApp.Services;

/// <summary>
/// Orchestrates all sale-related operations for the Sale_Manager component.
/// All reads and writes delegate exclusively to <see cref="IServiceClient"/>;
/// no EF Core, no direct database access.
/// </summary>
public interface ISaleManagerService
{
    /// <summary>
    /// Validates that the current cart is non-empty, builds a <see cref="CreateSaleRequest"/>
    /// from the cart items, submits it to the Backend_Service via
    /// <see cref="IServiceClient.CreateSaleAsync"/>, clears the cart on success, and returns
    /// the resulting <see cref="SaleResult"/>.
    /// </summary>
    /// <param name="request">
    ///   Checkout context: session (to read / clear the cart), staff user ID,
    ///   optional customer ID, and selected payment method.
    /// </param>
    /// <returns>A <see cref="SaleResult"/> containing the new <c>SaleID</c> and <c>InvoiceNo</c>.</returns>
    /// <exception cref="Infrastructure.Exceptions.CartValidationException">
    ///   Thrown when the cart is empty at the time of checkout.
    /// </exception>
    /// <exception cref="Infrastructure.Exceptions.SaleProcessingException">
    ///   Thrown when the Backend_Service rejects the sale (SOAP fault / business rule violation).
    /// </exception>
    Task<SaleResult> ProcessSaleAsync(ProcessSaleRequest request);

    /// <summary>
    /// Returns a paged list of sales filtered by the criteria in <paramref name="query"/>.
    /// <para>
    ///   Role filtering is applied here before forwarding to <see cref="IServiceClient"/>:
    ///   <list type="bullet">
    ///     <item>
    ///       <description>
    ///         When <c>query.UserLevel == "Staff"</c>, <c>SaleHistoryQuery.StaffUserID</c>
    ///         is populated so the Backend_Service limits results to that staff member's own sales.
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///         When <c>query.UserLevel</c> is <c>"Manager"</c> or <c>"Admin"</c>,
    ///         <c>SaleHistoryQuery.StaffUserID</c> is <see langword="null"/> so the
    ///         Backend_Service returns all sales.
    ///       </description>
    ///     </item>
    ///   </list>
    /// </para>
    /// </summary>
    Task<PagedResult<SaleListItemDto>> GetSaleHistoryAsync(SaleHistoryQuery query);

    /// <summary>
    /// Returns the full detail record for a single sale, including all line items
    /// and void status, by calling <see cref="IServiceClient.GetSaleDetailAsync"/>.
    /// </summary>
    Task<SaleDetailDto> GetSaleDetailAsync(int saleId);

    /// <summary>
    /// Voids the specified sale by calling <see cref="IServiceClient.VoidSaleAsync"/>.
    /// </summary>
    /// <param name="saleId">The identifier of the sale to void.</param>
    /// <param name="reason">A non-empty reason for the void.</param>
    /// <param name="staffUserId">The user ID of the staff member performing the void.</param>
    /// <returns><see langword="true"/> when the Backend_Service confirms the void.</returns>
    /// <exception cref="Infrastructure.Exceptions.CartValidationException">
    ///   Thrown when <paramref name="reason"/> is <see langword="null"/> or whitespace.
    /// </exception>
    Task<bool> VoidSaleAsync(int saleId, string reason, int staffUserId);
}
