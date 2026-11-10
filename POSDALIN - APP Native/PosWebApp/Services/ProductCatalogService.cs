using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using PosWebApp.Infrastructure;
using PosWebApp.Models.DTOs;

namespace PosWebApp.Services;

/// <summary>
/// Cache-first implementation of <see cref="IProductCatalogService"/>.
/// Uses a single stored procedure call (Sp_getSaleItem_Client2) to fetch ALL
/// products with category and group info, caches the full list in IMemoryCache,
/// and derives categories, groups, and items from the cached data.
///
/// Item detail and price tiers use separate stored procedure calls.
/// Search results are not cached (dynamic and paginated).
///
/// **Validates: Requirements 3.1, 3.2, 3.3, 4.1, 4.2, 4.3, 4.4, 4.5,
/// 10.2, 10.3, 10.7, 16.2, 16.3**
/// </summary>
public class ProductCatalogService : IProductCatalogService
{
    private const int MinSearchLength = 2;
    private const string AllProductsCacheKey = "catalog:allproducts";

    private readonly IServiceClient _serviceClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductCatalogService> _logger;
    private readonly TimeSpan _catalogSlidingExpiration;
    private readonly int _defaultPageSize;

    private readonly ConcurrentDictionary<string, byte> _cacheKeys = new();

    public ProductCatalogService(
        IServiceClient serviceClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<ProductCatalogService> logger)
    {
        _serviceClient = serviceClient;
        _cache = cache;
        _logger = logger;
        _catalogSlidingExpiration = TimeSpan.FromMinutes(
            configuration.GetValue<int>("Cache:CatalogSlidingExpirationMinutes", 60));
        _defaultPageSize = configuration.GetValue<int>("Catalog:ItemsPerPage", 50);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Single source: full product list from Sp_getSaleItem_Client2
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the full cached product list, fetching from the stored procedure
    /// on first call (cache miss). This is the single source of product data.
    /// </summary>
    private async Task<IReadOnlyList<ProductCatalogDto>> GetOrFetchAllProductsAsync()
    {
        if (_cache.TryGetValue<IReadOnlyList<ProductCatalogDto>>(AllProductsCacheKey, out var cached) && cached is not null)
        {
            _logger.LogDebug("Cache HIT for all products ({Count} items)", cached.Count);
            return cached;
        }

        _logger.LogInformation("Cache MISS for all products - calling Sp_getSaleItem_Client2");
        var products = await _serviceClient.GetAllProductsAsync();

        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = _catalogSlidingExpiration
        };
        _cache.Set(AllProductsCacheKey, products, options);
        _cacheKeys.TryAdd(AllProductsCacheKey, 0);

        return products;
    }

    /// <summary>
    /// Public method to get all products (exposes the cached full product list).
    /// </summary>
    public async Task<IReadOnlyList<ProductCatalogDto>> GetAllProductsAsync()
    {
        return await GetOrFetchAllProductsAsync();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Categories (derived from cached full product list)
    // ════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync()
    {
        _logger.LogInformation("GetCategoriesAsync - deriving from cached product list");
        var products = await GetOrFetchAllProductsAsync();

        return products
            .Select(p => new { p.CategoryID, p.CategoryName })
            .Distinct()
            .Select(c => new CategoryDto(c.CategoryID, c.CategoryName, ""))
            .ToList();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Groups (derived from cached full product list, filtered by category)
    // ════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<GroupDto>> GetGroupsAsync(int categoryId)
    {
        _logger.LogInformation("GetGroupsAsync - categoryId: {CategoryId}", categoryId);
        var products = await GetOrFetchAllProductsAsync();

        return products
            .Where(p => p.CategoryID == categoryId)
            .Select(p => new { p.GroupID, p.GroupName, p.CategoryID })
            .Distinct()
            .Select(g => new GroupDto(g.GroupID, g.GroupName, "", g.CategoryID))
            .ToList();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Items (derived from cached full product list, filtered by category/group)
    // ════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<ItemDto>> GetItemsAsync(int categoryId, int groupId)
    {
        _logger.LogInformation("GetItemsAsync - categoryId: {CategoryId}, groupId: {GroupId}", categoryId, groupId);
        var products = await GetOrFetchAllProductsAsync();

        return products
            .Where(p => p.CategoryID == categoryId && (groupId == 0 || p.GroupID == groupId))
            .Select(p => new ItemDto(
                p.ItemID, p.ItemCode, p.ItemName, p.ImageUrl, p.Price, p.CategoryID, p.GroupID))
            .ToList();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Item detail (separate stored procedure call)
    // ════════════════════════════════════════════════════════════════════════

    public async Task<ItemDetailDto> GetItemDetailAsync(int itemId)
    {
        _logger.LogInformation("GetItemDetailAsync - itemId: {ItemId}", itemId);
        return await GetOrSetAsync(
            CacheKeys.ItemDetail(itemId),
            () => _serviceClient.GetItemDetailAsync(itemId));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Price tiers (separate stored procedure call)
    // ════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<PriceTierDto>> GetPriceTiersAsync(int itemId)
    {
        _logger.LogInformation("GetPriceTiersAsync - itemId: {ItemId}", itemId);
        return await GetOrSetAsync(
            CacheKeys.PriceTiers(itemId),
            () => _serviceClient.GetItemUnitsAsync(itemId));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Search (filters from cached product list)
    // ════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<ItemDto>> SearchItemsAsync(string query, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < MinSearchLength)
            return Array.Empty<ItemDto>();

        _logger.LogInformation("SearchItemsAsync - query: {Query}, page: {Page}, pageSize: {PageSize}", 
            query, page, pageSize);

        // Get all products from cache
        var products = await GetOrFetchAllProductsAsync();
        
        var effectivePageSize = pageSize > 0 ? pageSize : _defaultPageSize;
        var searchTerm = query.Trim();

        // Filter by ItemName or ItemCode (case-insensitive)
        var filtered = products
            .Where(p => 
                (p.ItemName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.ItemCode?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
            .Skip((page - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .Select(p => new ItemDto(
                p.ItemID, p.ItemCode, p.ItemName, p.ImageUrl, p.Price, p.CategoryID, p.GroupID))
            .ToList();

        _logger.LogInformation("SearchItemsAsync - found {Count} results for query: {Query}", 
            filtered.Count, query);

        return filtered;
    }

    public static IReadOnlyList<ItemDto> SearchPure(IReadOnlyList<ItemDto> items, string query)
    {
        if (items is null || items.Count == 0 || string.IsNullOrWhiteSpace(query))
            return Array.Empty<ItemDto>();

        var q = query.Trim();
        return items.Where(i =>
            i.ItemName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            i.ItemCode.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cache invalidation
    // ════════════════════════════════════════════════════════════════════════

    public Task InvalidateCatalogCacheAsync()
    {
        _logger.LogInformation("InvalidateCatalogCacheAsync - evicting {Count} cache entries", _cacheKeys.Count);
        foreach (var key in _cacheKeys.Keys)
            _cache.Remove(key);
        _cacheKeys.Clear();
        return Task.CompletedTask;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Private helpers
    // ════════════════════════════════════════════════════════════════════════

    private async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> fetchFactory) where T : class
    {
        if (_cache.TryGetValue<T>(key, out var cached) && cached is not null)
            return cached;

        var value = await fetchFactory();
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            SlidingExpiration = _catalogSlidingExpiration
        });
        _cacheKeys.TryAdd(key, 0);
        return value;
    }
}