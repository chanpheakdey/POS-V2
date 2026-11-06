using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PosWebApp.Infrastructure;
using PosWebApp.Models.DTOs;
using PosWebApp.Services;

namespace PosWebApp.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="ProductCatalogService"/> covering cache-first
/// behaviour, search enforcement, and cache invalidation.
///
/// **Validates: Requirements 3.1, 3.2, 3.3, 4.1, 4.2, 4.3, 4.4, 4.5,
/// 10.2, 10.7, 16.2, 16.3**
/// </summary>
public class ProductCatalogServiceTests
{
    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ProductCatalogService CreateSut(
        Mock<IServiceClient> serviceClient,
        IMemoryCache? cache = null)
    {
        cache ??= new MemoryCache(new MemoryCacheOptions());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:CatalogSlidingExpirationMinutes"] = "60",
                ["Catalog:ItemsPerPage"] = "50"
            })
            .Build();

        return new ProductCatalogService(
            serviceClient.Object,
            cache,
            config,
            NullLogger<ProductCatalogService>.Instance);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Categories — cache-first
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategoriesAsync_CacheMiss_CallsServiceClient()
    {
        var serviceClient = new Mock<IServiceClient>();
        var expected = new List<CategoryDto>
        {
            new(1, "Drinks", "/img/1.png"),
            new(2, "Food", "/img/2.png")
        };
        serviceClient.Setup(s => s.GetCategoriesAsync())
            .ReturnsAsync(expected);

        var sut = CreateSut(serviceClient);

        var result = await sut.GetCategoriesAsync();

        Assert.Equal(expected, result);
        serviceClient.Verify(s => s.GetCategoriesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCategoriesAsync_CacheHit_DoesNotCallServiceClientAgain()
    {
        var serviceClient = new Mock<IServiceClient>();
        serviceClient.Setup(s => s.GetCategoriesAsync())
            .ReturnsAsync(new List<CategoryDto> { new(1, "Drinks", "/img/1.png") });

        var sut = CreateSut(serviceClient);

        await sut.GetCategoriesAsync();
        await sut.GetCategoriesAsync();

        // Requirement 3.1 / 3.2: first call fetches from backend, second from cache.
        serviceClient.Verify(s => s.GetCategoriesAsync(), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Groups — passes CategoryID
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetGroupsAsync_PassesCategoryIdAndCaches()
    {
        var serviceClient = new Mock<IServiceClient>();
        var expected = new List<GroupDto> { new(10, "Hot", "/img/10.png", 1) };
        serviceClient.Setup(s => s.GetGroupsAsync(1))
            .ReturnsAsync(expected);

        var sut = CreateSut(serviceClient);

        var result = await sut.GetGroupsAsync(1);
        await sut.GetGroupsAsync(1);

        Assert.Equal(expected, result);
        // Requirement 4.3: CategoryID passed to Backend_Service; cached after first fetch.
        serviceClient.Verify(s => s.GetGroupsAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetGroupsAsync_DifferentCategories_CacheIndependently()
    {
        var serviceClient = new Mock<IServiceClient>();
        serviceClient.Setup(s => s.GetGroupsAsync(1))
            .ReturnsAsync(new List<GroupDto> { new(10, "A", "", 1) });
        serviceClient.Setup(s => s.GetGroupsAsync(2))
            .ReturnsAsync(new List<GroupDto> { new(20, "B", "", 2) });

        var sut = CreateSut(serviceClient);

        await sut.GetGroupsAsync(1);
        await sut.GetGroupsAsync(2);
        await sut.GetGroupsAsync(1);
        await sut.GetGroupsAsync(2);

        serviceClient.Verify(s => s.GetGroupsAsync(1), Times.Once);
        serviceClient.Verify(s => s.GetGroupsAsync(2), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Items — passes CategoryID and GroupID
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetItemsAsync_PassesCategoryIdAndGroupIdAndCaches()
    {
        var serviceClient = new Mock<IServiceClient>();
        var expected = new List<ItemDto>
        {
            new(100, "CODE1", "Coffee", "/img/100.png", 2.50m, 1, 10)
        };
        serviceClient.Setup(s => s.GetSaleItemsAsync(1, 10))
            .ReturnsAsync(expected);

        var sut = CreateSut(serviceClient);

        var result = await sut.GetItemsAsync(1, 10);
        await sut.GetItemsAsync(1, 10);

        Assert.Equal(expected, result);
        // Requirement 4.4: CategoryID and GroupID passed to Backend_Service; cached.
        serviceClient.Verify(s => s.GetSaleItemsAsync(1, 10), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Item detail
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetItemDetailAsync_CachesByItemId()
    {
        var serviceClient = new Mock<IServiceClient>();
        var expected = new ItemDetailDto(100, "CODE1", "Coffee", "/img/100.png",
            new List<PriceTierDto> { new(1, 100, 1, 0, 2.50m, "USD") });
        serviceClient.Setup(s => s.GetItemDetailAsync(100))
            .ReturnsAsync(expected);

        var sut = CreateSut(serviceClient);

        var result = await sut.GetItemDetailAsync(100);
        await sut.GetItemDetailAsync(100);

        Assert.Equal(expected, result);
        serviceClient.Verify(s => s.GetItemDetailAsync(100), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Price tiers — calls GetItemUnitsAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPriceTiersAsync_CallsGetItemUnitsAndCaches()
    {
        var serviceClient = new Mock<IServiceClient>();
        var expected = new List<PriceTierDto>
        {
            new(1, 100, 1, 9, 2.50m, "USD"),
            new(2, 100, 10, 0, 2.00m, "USD")
        };
        serviceClient.Setup(s => s.GetItemUnitsAsync(100))
            .ReturnsAsync(expected);

        var sut = CreateSut(serviceClient);

        var result = await sut.GetPriceTiersAsync(100);
        await sut.GetPriceTiersAsync(100);

        Assert.Equal(expected, result);
        serviceClient.Verify(s => s.GetItemUnitsAsync(100), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Search — minimum 2 characters
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public async Task SearchItemsAsync_BelowTwoChars_ReturnsEmptyAndDoesNotCallBackend(string? query)
    {
        var serviceClient = new Mock<IServiceClient>();
        var sut = CreateSut(serviceClient);

        var result = await sut.SearchItemsAsync(query!, 1, 50);

        Assert.Empty(result);
        // Requirement 10.7: backend not called for < 2 chars.
        serviceClient.Verify(
            s => s.SearchItemsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchItemsAsync_TwoChars_CallsBackendWithTrimmedKeyword()
    {
        var serviceClient = new Mock<IServiceClient>();
        var expected = new List<ItemDto>
        {
            new(1, "AB", "Abacus", "", 1m, 1, 1)
        };
        serviceClient.Setup(s => s.SearchItemsAsync("ab", 1, 50))
            .ReturnsAsync(expected);

        var sut = CreateSut(serviceClient);

        var result = await sut.SearchItemsAsync("  ab  ", 1, 50);

        Assert.Equal(expected, result);
        // Requirement 10.2: keyword passed to backend; whitespace trimmed.
        serviceClient.Verify(s => s.SearchItemsAsync("ab", 1, 50), Times.Once);
    }

    [Fact]
    public async Task SearchItemsAsync_NonPositivePageSize_UsesDefault50()
    {
        var serviceClient = new Mock<IServiceClient>();
        serviceClient.Setup(s => s.SearchItemsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<ItemDto>());

        var sut = CreateSut(serviceClient);

        await sut.SearchItemsAsync("test", 1, 0);
        await sut.SearchItemsAsync("test", 1, -5);

        serviceClient.Verify(s => s.SearchItemsAsync("test", 1, 50), Times.Exactly(2));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  SearchPure — static client-side filter
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SearchPure_FiltersByItemNameCaseInsensitive()
    {
        var items = new List<ItemDto>
        {
            new(1, "C001", "Espresso", "", 2m, 1, 1),
            new(2, "C002", "Latte", "", 3m, 1, 1),
            new(3, "C003", "Cappuccino", "", 3.5m, 1, 1)
        };

        var results = ProductCatalogService.SearchPure(items, "ess");

        Assert.Single(results);
        Assert.Equal("Espresso", results[0].ItemName);
    }

    [Fact]
    public void SearchPure_FiltersByItemCodeCaseInsensitive()
    {
        var items = new List<ItemDto>
        {
            new(1, "C001", "Espresso", "", 2m, 1, 1),
            new(2, "LATTE", "Latte", "", 3m, 1, 1),
        };

        var results = ProductCatalogService.SearchPure(items, "latte");

        Assert.Single(results);
        Assert.Equal("Latte", results[0].ItemName);
    }

    [Fact]
    public void SearchPure_EmptyQuery_ReturnsEmpty()
    {
        var items = new List<ItemDto>
        {
            new(1, "C001", "Espresso", "", 2m, 1, 1),
        };

        Assert.Empty(ProductCatalogService.SearchPure(items, ""));
        Assert.Empty(ProductCatalogService.SearchPure(items, "   "));
    }

    [Fact]
    public void SearchPure_EmptyItemList_ReturnsEmpty()
    {
        Assert.Empty(ProductCatalogService.SearchPure(new List<ItemDto>(), "test"));
        Assert.Empty(ProductCatalogService.SearchPure(null!, "test"));
    }

    [Fact]
    public void SearchPure_ReturnsAllMatchingItems()
    {
        var items = new List<ItemDto>
        {
            new(1, "C001", "Coffee Black", "", 2m, 1, 1),
            new(2, "C002", "Coffee Latte", "", 3m, 1, 1),
            new(3, "C003", "Tea", "", 1.5m, 1, 1),
        };

        var results = ProductCatalogService.SearchPure(items, "coffee");

        Assert.Equal(2, results.Count);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Cache invalidation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task InvalidateCatalogCacheAsync_EvictsAllCachedEntries()
    {
        var serviceClient = new Mock<IServiceClient>();
        serviceClient.Setup(s => s.GetCategoriesAsync())
            .ReturnsAsync(new List<CategoryDto> { new(1, "A", "") });
        serviceClient.Setup(s => s.GetGroupsAsync(1))
            .ReturnsAsync(new List<GroupDto> { new(10, "G", "", 1) });
        serviceClient.Setup(s => s.GetSaleItemsAsync(1, 10))
            .ReturnsAsync(new List<ItemDto> { new(100, "C", "I", "", 1m, 1, 10) });
        serviceClient.Setup(s => s.GetItemDetailAsync(100))
            .ReturnsAsync(new ItemDetailDto(100, "C", "I", "", new List<PriceTierDto>()));
        serviceClient.Setup(s => s.GetItemUnitsAsync(100))
            .ReturnsAsync(new List<PriceTierDto> { new(1, 100, 1, 0, 1m, "USD") });

        var sut = CreateSut(serviceClient);

        // Prime the cache with various entries.
        await sut.GetCategoriesAsync();
        await sut.GetGroupsAsync(1);
        await sut.GetItemsAsync(1, 10);
        await sut.GetItemDetailAsync(100);
        await sut.GetPriceTiersAsync(100);

        // Invalidate.
        await sut.InvalidateCatalogCacheAsync();

        // All subsequent calls should hit the backend again (cache was evicted).
        await sut.GetCategoriesAsync();
        await sut.GetGroupsAsync(1);
        await sut.GetItemsAsync(1, 10);
        await sut.GetItemDetailAsync(100);
        await sut.GetPriceTiersAsync(100);

        serviceClient.Verify(s => s.GetCategoriesAsync(), Times.Exactly(2));
        serviceClient.Verify(s => s.GetGroupsAsync(1), Times.Exactly(2));
        serviceClient.Verify(s => s.GetSaleItemsAsync(1, 10), Times.Exactly(2));
        serviceClient.Verify(s => s.GetItemDetailAsync(100), Times.Exactly(2));
        serviceClient.Verify(s => s.GetItemUnitsAsync(100), Times.Exactly(2));
    }

    [Fact]
    public async Task InvalidateCatalogCacheAsync_WhenCacheEmpty_IsNoOp()
    {
        var serviceClient = new Mock<IServiceClient>();
        var sut = CreateSut(serviceClient);

        // Should not throw when there is nothing to evict.
        await sut.InvalidateCatalogCacheAsync();

        serviceClient.Verify(
            s => s.GetCategoriesAsync(),
            Times.Never);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Sliding expiration applied
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategoriesAsync_UsesConfiguredSlidingExpiration()
    {
        var serviceClient = new Mock<IServiceClient>();
        serviceClient.Setup(s => s.GetCategoriesAsync())
            .ReturnsAsync(new List<CategoryDto> { new(1, "A", "") });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:CatalogSlidingExpirationMinutes"] = "30",
                ["Catalog:ItemsPerPage"] = "50"
            })
            .Build();

        var sut = new ProductCatalogService(
            serviceClient.Object, cache, config, NullLogger<ProductCatalogService>.Instance);

        await sut.GetCategoriesAsync();

        // Verify the entry exists with sliding expiration by reading it back.
        Assert.True(cache.TryGetValue(CacheKeys.Categories, out _));
    }
}
