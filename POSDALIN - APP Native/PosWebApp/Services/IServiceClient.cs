using PosWebApp.Models.DTOs;

namespace PosWebApp.Services;

/// <summary>
/// Abstraction over the ASMX Backend_Service.
/// All methods are async and wrap every failure in a
/// <see cref="PosWebApp.Infrastructure.Exceptions.ServiceException"/>.
/// </summary>
public interface IServiceClient
{
    // ── Authentication ──────────────────────────────────────────────────────

    /// <summary>Validates staff credentials against the Backend_Service login endpoint.</summary>
    Task<LoginResult> LoginAsync(string username, string password);

    // ── Product catalog ─────────────────────────────────────────────────────

    /// <summary>Returns all active categories.</summary>
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync();

    /// <summary>Returns all groups that belong to <paramref name="categoryId"/>.</summary>
    Task<IReadOnlyList<GroupDto>> GetGroupsAsync(int categoryId);

    /// <summary>Returns items for the given category / group combination.</summary>
    Task<IReadOnlyList<ItemDto>> GetSaleItemsAsync(int categoryId, int groupId);

    /// <summary>Returns the full detail record for a single item.</summary>
    Task<ItemDetailDto> GetItemDetailAsync(int itemId);

    /// <summary>Returns all price tiers for the given item.</summary>
    Task<IReadOnlyList<PriceTierDto>> GetItemUnitsAsync(int itemId);

    /// <summary>Full-text searches items by name or code with server-side pagination.</summary>
    Task<IReadOnlyList<ItemDto>> SearchItemsAsync(string keyword, int page, int pageSize);

    // ── Sales ────────────────────────────────────────────────────────────────

    /// <summary>Submits a new sale to the Backend_Service and returns the created SaleID / InvoiceNo.</summary>
    Task<SaleCreationResult> CreateSaleAsync(CreateSaleRequest request);

    /// <summary>Returns a paged list of sales, filtered by the criteria in <paramref name="query"/>.</summary>
    Task<PagedResult<SaleListItemDto>> GetSaleHistoryAsync(SaleHistoryQuery query);

    /// <summary>Returns the full detail record for a single sale including all line items.</summary>
    Task<SaleDetailDto> GetSaleDetailAsync(int saleId);

    /// <summary>Voids the specified sale. Returns <c>true</c> on success.</summary>
    Task<bool> VoidSaleAsync(int saleId, string reason, int staffUserId);

    // ── Customers ─────────────────────────────────────────────────────────────

    /// <summary>Searches customers by name or phone number.</summary>
    Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string query);

    // ── Full catalog (single source) ──────────────────────────────────────────

    /// <summary>
    /// Returns ALL products with category and group info in a single call
    /// via Sp_getSaleItem_Client2 stored procedure. This is the single source
    /// of product data that should be cached on the server.
    /// </summary>
    Task<IReadOnlyList<ProductCatalogDto>> GetAllProductsAsync();

    // ── Images ───────────────────────────────────────────────────────────────

    /// <summary>Returns the raw image stream from the Backend_Service hdImage_all.ashx endpoint.</summary>
    Task<Stream> GetImageStreamAsync(string imagePath);
}
