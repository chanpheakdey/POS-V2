using PosWebApp.Models.DTOs;

namespace PosWebApp.Services;

/// <summary>
/// Provides cached access to the product catalog (categories, groups, items,
/// item details, and price tiers) and item search. All data is fetched from the
/// Backend_Service via <see cref="IServiceClient"/> and cached in-process using
/// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> with a 1-hour
/// sliding expiration.
/// </summary>
public interface IProductCatalogService
{
    /// <summary>
    /// Returns all active categories, reading from cache first and falling back
    /// to the Backend_Service getcategory endpoint on a cache miss.
    /// </summary>
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync();

    /// <summary>
    /// Returns all groups that belong to <paramref name="categoryId"/>, reading
    /// from cache first and falling back to the Backend_Service getgroup endpoint
    /// on a cache miss.
    /// </summary>
    Task<IReadOnlyList<GroupDto>> GetGroupsAsync(int categoryId);

    /// <summary>
    /// Returns items for the given category / group combination, reading from
    /// cache first and falling back to the Backend_Service getSaleItem endpoint
    /// on a cache miss.
    /// </summary>
    Task<IReadOnlyList<ItemDto>> GetItemsAsync(int categoryId, int groupId);

    /// <summary>
    /// Returns the full detail record for a single item, reading from cache first
    /// and falling back to the Backend_Service getitemdetail endpoint on a cache
    /// miss.
    /// </summary>
    Task<ItemDetailDto> GetItemDetailAsync(int itemId);

    /// <summary>
    /// Returns all price tiers for the given item, reading from cache first and
    /// falling back to the Backend_Service getitemunit endpoint on a cache miss.
    /// </summary>
    Task<IReadOnlyList<PriceTierDto>> GetPriceTiersAsync(int itemId);

    /// <summary>
    /// Searches items by name or code with server-side pagination. The
    /// Backend_Service searchItems endpoint is called only when the query is at
    /// least 2 characters long; shorter queries return an empty result set
    /// without contacting the backend.
    /// </summary>
    Task<IReadOnlyList<ItemDto>> SearchItemsAsync(string query, int page, int pageSize);

    /// <summary>
    /// Evicts all cached catalog entries (categories, groups, items, item
    /// details, and price tiers).
    /// </summary>
    Task InvalidateCatalogCacheAsync();
}
