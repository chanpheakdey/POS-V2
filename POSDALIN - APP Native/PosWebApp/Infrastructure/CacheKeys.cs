namespace PosWebApp.Infrastructure;

/// <summary>
/// Centralised string constants for IMemoryCache keys used throughout the application.
/// Parameterised keys use the <c>For*</c> factory methods to guarantee consistent formatting.
/// </summary>
public static class CacheKeys
{
    /// <summary>All product categories.</summary>
    public const string Categories = "catalog:categories";

    // ── Parameterised factory methods ────────────────────────────────────────

    /// <summary>Groups belonging to <paramref name="categoryId"/>.</summary>
    public static string GroupsForCategory(int categoryId)
        => $"catalog:groups:{categoryId}";

    /// <summary>Items belonging to <paramref name="categoryId"/> and <paramref name="groupId"/>.</summary>
    public static string ItemsForCategoryGroup(int categoryId, int groupId)
        => $"catalog:items:{categoryId}:{groupId}";

    /// <summary>Full detail for a single item.</summary>
    public static string ItemDetail(int itemId)
        => $"catalog:item:{itemId}";

    /// <summary>Price tiers for a single item.</summary>
    public static string PriceTiers(int itemId)
        => $"catalog:tiers:{itemId}";
}
