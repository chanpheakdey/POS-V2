using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Caching.Memory;
using PosWebApp.Infrastructure;
using PosWebApp.Models.DTOs;

namespace PosWebApp.Tests.Unit.Properties;

/// <summary>
/// Property-based tests for cache round-trip integrity of catalog entities stored
/// in <see cref="IMemoryCache"/>.
///
/// **Validates: Requirements 3.1, 3.2, 3.3, 4.5**
/// </summary>
public class CachePropertyTests : IDisposable
{
    private readonly MemoryCache _cache;

    public CachePropertyTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public void Dispose() => _cache.Dispose();

    // ── Arbitrary / generators ────────────────────────────────────────────────

    /// <summary>
    /// Generates an arbitrary <see cref="CategoryDto"/> with constrained field values.
    /// CategoryID: 1..9999, CategoryName: non-null non-empty, ImageUrl: non-null.
    /// </summary>
    public static Arbitrary<CategoryDto> CategoryDtoArb()
    {
        var gen =
            from id   in Gen.Choose(1, 9999)
            from name in ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get)
            from url  in ArbMap.Default.ArbFor<string>().Generator.Select(s => s ?? string.Empty)
            select new CategoryDto(id, name, url);
        return gen.ToArbitrary();
    }

    /// <summary>
    /// Generates an arbitrary <see cref="GroupDto"/> with constrained field values.
    /// </summary>
    public static Arbitrary<GroupDto> GroupDtoArb()
    {
        var gen =
            from id         in Gen.Choose(1, 9999)
            from categoryId in Gen.Choose(1, 9999)
            from name       in ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get)
            from url        in ArbMap.Default.ArbFor<string>().Generator.Select(s => s ?? string.Empty)
            select new GroupDto(id, name, url, categoryId);
        return gen.ToArbitrary();
    }

    /// <summary>
    /// Generates an arbitrary <see cref="ItemDto"/> with constrained field values.
    /// Price is kept in a sane range (0.01 to 99,999.99).
    /// </summary>
    public static Arbitrary<ItemDto> ItemDtoArb()
    {
        var gen =
            from id         in Gen.Choose(1, 9999)
            from categoryId in Gen.Choose(1, 9999)
            from groupId    in Gen.Choose(1, 9999)
            from code       in ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get)
            from name       in ArbMap.Default.ArbFor<NonEmptyString>().Generator.Select(s => s.Get)
            from url        in ArbMap.Default.ArbFor<string>().Generator.Select(s => s ?? string.Empty)
            from priceCents in Gen.Choose(1, 9_999_999)
            select new ItemDto(id, code, name, url, (decimal)priceCents / 100m, categoryId, groupId);
        return gen.ToArbitrary();
    }

    /// <summary>
    /// Generates an arbitrary <see cref="PriceTierDto"/> with constrained fields.
    /// FromQty: 1..500; ToQty: 0 (open-ended) or FromQty..FromQty+499; Price: 0.01..99,999.99.
    /// </summary>
    public static Arbitrary<PriceTierDto> PriceTierDtoArb()
    {
        var gen =
            from id         in Gen.Choose(1, 9999)
            from itemId     in Gen.Choose(1, 9999)
            from fromQty    in Gen.Choose(1, 500)
            from openEnded  in Gen.Choose(0, 1)
            from span       in Gen.Choose(1, 499)
            from priceCents in Gen.Choose(1, 9_999_999)
            let toQty    = openEnded == 1 ? 0 : fromQty + span - 1
            let currency = "USD"
            select new PriceTierDto(id, itemId, fromQty, toQty, (decimal)priceCents / 100m, currency);
        return gen.ToArbitrary();
    }

    // ── Property 6: Cache Round-Trip Integrity ────────────────────────────────

    /// <summary>
    /// **Property 6: Cache Round-Trip Integrity — CategoryDto**
    ///
    /// For any <see cref="CategoryDto"/>, storing it in <see cref="IMemoryCache"/> under
    /// <see cref="CacheKeys.Categories"/> and immediately reading it back (before expiry)
    /// SHALL return a value with deep equality to what was stored.
    ///
    /// **Validates: Requirements 3.1, 3.2, 3.3**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CachePropertyTests) }, MaxTest = 500)]
    public Property Property6_CategoryDtoRoundTrip(CategoryDto dto)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var entryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        var list = new List<CategoryDto> { dto };
        cache.Set(CacheKeys.Categories, list, entryOptions);
        var retrieved = cache.Get<List<CategoryDto>>(CacheKeys.Categories);

        bool notNull      = retrieved is not null;
        bool singleEntry  = notNull && retrieved!.Count == 1;
        bool deepEqual    = singleEntry && retrieved![0] == dto;

        return deepEqual.Label(
            $"CategoryDto round-trip failed. " +
            $"Stored: ({dto.CategoryID}, \"{dto.CategoryName}\", \"{dto.ImageUrl}\"), " +
            $"Retrieved: {(retrieved is null ? "null" : $"count={retrieved.Count}")}");
    }

    /// <summary>
    /// **Property 6: Cache Round-Trip Integrity — GroupDto**
    ///
    /// For any <see cref="GroupDto"/>, storing it under
    /// <c>CacheKeys.GroupsForCategory(categoryId)</c> and reading it back before expiry
    /// SHALL return a value with deep equality.
    ///
    /// **Validates: Requirements 3.1, 4.5**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CachePropertyTests) }, MaxTest = 500)]
    public Property Property6_GroupDtoRoundTrip(GroupDto dto)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var entryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        string key  = CacheKeys.GroupsForCategory(dto.CategoryID);
        var list    = new List<GroupDto> { dto };
        cache.Set(key, list, entryOptions);
        var retrieved = cache.Get<List<GroupDto>>(key);

        bool notNull    = retrieved is not null;
        bool singleEntry = notNull && retrieved!.Count == 1;
        bool deepEqual  = singleEntry && retrieved![0] == dto;

        return deepEqual.Label(
            $"GroupDto round-trip failed under key \"{key}\". " +
            $"Stored: ({dto.GroupID}, \"{dto.GroupName}\", categoryId={dto.CategoryID}), " +
            $"Retrieved: {(retrieved is null ? "null" : $"count={retrieved.Count}")}");
    }

    /// <summary>
    /// **Property 6: Cache Round-Trip Integrity — ItemDto**
    ///
    /// For any <see cref="ItemDto"/>, storing it under
    /// <c>CacheKeys.ItemsForCategoryGroup(categoryId, groupId)</c> and reading it back
    /// before expiry SHALL return a value with deep equality.
    ///
    /// **Validates: Requirements 3.2, 3.3, 4.5**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CachePropertyTests) }, MaxTest = 500)]
    public Property Property6_ItemDtoRoundTrip(ItemDto dto)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var entryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        string key  = CacheKeys.ItemsForCategoryGroup(dto.CategoryID, dto.GroupID);
        var list    = new List<ItemDto> { dto };
        cache.Set(key, list, entryOptions);
        var retrieved = cache.Get<List<ItemDto>>(key);

        bool notNull     = retrieved is not null;
        bool singleEntry = notNull && retrieved!.Count == 1;
        bool deepEqual   = singleEntry && retrieved![0] == dto;

        return deepEqual.Label(
            $"ItemDto round-trip failed under key \"{key}\". " +
            $"Stored: ({dto.ItemID}, \"{dto.ItemCode}\", price={dto.Price}), " +
            $"Retrieved: {(retrieved is null ? "null" : $"count={retrieved.Count}")}");
    }

    /// <summary>
    /// **Property 6: Cache Round-Trip Integrity — PriceTierDto**
    ///
    /// For any <see cref="PriceTierDto"/>, storing it under
    /// <c>CacheKeys.PriceTiers(itemId)</c> and reading it back before expiry
    /// SHALL return a value with deep equality.
    ///
    /// **Validates: Requirements 4.5**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CachePropertyTests) }, MaxTest = 500)]
    public Property Property6_PriceTierDtoRoundTrip(PriceTierDto dto)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var entryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        string key  = CacheKeys.PriceTiers(dto.ItemID);
        var list    = new List<PriceTierDto> { dto };
        cache.Set(key, list, entryOptions);
        var retrieved = cache.Get<List<PriceTierDto>>(key);

        bool notNull     = retrieved is not null;
        bool singleEntry = notNull && retrieved!.Count == 1;
        bool deepEqual   = singleEntry && retrieved![0] == dto;

        return deepEqual.Label(
            $"PriceTierDto round-trip failed under key \"{key}\". " +
            $"Stored: (id={dto.ID}, itemId={dto.ItemID}, from={dto.FromQty}, to={dto.ToQty}, " +
            $"price={dto.Price}, currency=\"{dto.Currency}\"), " +
            $"Retrieved: {(retrieved is null ? "null" : $"count={retrieved.Count}")}");
    }

    /// <summary>
    /// **Property 6: Cache Round-Trip Integrity — distinct keys do not collide**
    ///
    /// Storing two different catalog entities under their respective distinct keys SHALL
    /// not cause either key to return the other's value. Keys generated by
    /// <see cref="CacheKeys"/> factory methods are unique per (type × ids).
    ///
    /// **Validates: Requirements 3.1, 3.2, 3.3, 4.5**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(CachePropertyTests) }, MaxTest = 300)]
    public Property Property6_DistinctEntityKeysDontCollide(GroupDto dto)
    {
        // Use two different category IDs to ensure keys differ
        int catId1 = dto.CategoryID;
        int catId2 = dto.CategoryID == 9999 ? 1 : dto.CategoryID + 1;

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var entryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        string key1 = CacheKeys.GroupsForCategory(catId1);
        string key2 = CacheKeys.GroupsForCategory(catId2);

        var list1 = new List<GroupDto> { dto with { CategoryID = catId1 } };
        var list2 = new List<GroupDto> { dto with { CategoryID = catId2 } };

        cache.Set(key1, list1, entryOptions);
        cache.Set(key2, list2, entryOptions);

        var retrieved1 = cache.Get<List<GroupDto>>(key1);
        var retrieved2 = cache.Get<List<GroupDto>>(key2);

        bool key1IsCorrect = retrieved1 is not null &&
                             retrieved1.Count == 1 &&
                             retrieved1[0].CategoryID == catId1;
        bool key2IsCorrect = retrieved2 is not null &&
                             retrieved2.Count == 1 &&
                             retrieved2[0].CategoryID == catId2;

        return (key1IsCorrect && key2IsCorrect).Label(
            $"Distinct keys should not collide. " +
            $"key1=\"{key1}\" ok={key1IsCorrect}, key2=\"{key2}\" ok={key2IsCorrect}");
    }

    // ── Example-based edge cases ──────────────────────────────────────────────

    [Fact]
    public void CategoryList_StoredAndRetrieved_AreEqual()
    {
        var categories = new List<CategoryDto>
        {
            new(1, "Beverages",  "http://example.com/bev.jpg"),
            new(2, "Food",       "http://example.com/food.jpg"),
        };

        _cache.Set(CacheKeys.Categories, categories, TimeSpan.FromHours(1));
        var result = _cache.Get<List<CategoryDto>>(CacheKeys.Categories);

        Assert.NotNull(result);
        Assert.Equal(categories.Count, result!.Count);
        Assert.Equal(categories[0], result[0]);
        Assert.Equal(categories[1], result[1]);
    }

    [Fact]
    public void GroupList_StoredUnderCorrectKey_RoundTripsSuccessfully()
    {
        const int categoryId = 42;
        var groups = new List<GroupDto>
        {
            new(10, "Hot Drinks",  "http://example.com/hot.jpg",  categoryId),
            new(11, "Cold Drinks", "http://example.com/cold.jpg", categoryId),
        };

        string key = CacheKeys.GroupsForCategory(categoryId);
        _cache.Set(key, groups, TimeSpan.FromHours(1));
        var result = _cache.Get<List<GroupDto>>(key);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal(groups[0], result[0]);
        Assert.Equal(groups[1], result[1]);
    }

    [Fact]
    public void ItemList_StoredUnderCorrectKey_RoundTripsSuccessfully()
    {
        const int categoryId = 5;
        const int groupId    = 17;
        var items = new List<ItemDto>
        {
            new(100, "ITEM-001", "Espresso",   "http://example.com/esp.jpg",   3.50m, categoryId, groupId),
            new(101, "ITEM-002", "Cappuccino", "http://example.com/cap.jpg",   4.00m, categoryId, groupId),
        };

        string key = CacheKeys.ItemsForCategoryGroup(categoryId, groupId);
        _cache.Set(key, items, TimeSpan.FromHours(1));
        var result = _cache.Get<List<ItemDto>>(key);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal(items[0], result[0]);
        Assert.Equal(items[1], result[1]);
    }

    [Fact]
    public void PriceTierList_StoredUnderCorrectKey_RoundTripsSuccessfully()
    {
        const int itemId = 100;
        var tiers = new List<PriceTierDto>
        {
            new(1, itemId, 1,  9,  5.00m, "USD"),
            new(2, itemId, 10, 0,  3.50m, "USD"),
        };

        string key = CacheKeys.PriceTiers(itemId);
        _cache.Set(key, tiers, TimeSpan.FromHours(1));
        var result = _cache.Get<List<PriceTierDto>>(key);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal(tiers[0], result[0]);
        Assert.Equal(tiers[1], result[1]);
    }

    [Fact]
    public void MissingKey_ReturnsNull()
    {
        var result = _cache.Get<List<CategoryDto>>("catalog:categories:nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void TwoCategories_StoredUnderSameKey_SecondOverwritesFirst()
    {
        var first  = new List<CategoryDto> { new(1, "First",  "") };
        var second = new List<CategoryDto> { new(2, "Second", "") };

        _cache.Set(CacheKeys.Categories, first,  TimeSpan.FromHours(1));
        _cache.Set(CacheKeys.Categories, second, TimeSpan.FromHours(1));
        var result = _cache.Get<List<CategoryDto>>(CacheKeys.Categories);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal(second[0], result[0]);
    }
}
