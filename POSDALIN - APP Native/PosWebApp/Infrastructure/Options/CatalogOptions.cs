namespace PosWebApp.Infrastructure.Options;

/// <summary>
/// Strongly-typed binding for the <c>Catalog</c> section in appsettings.json.
/// Provides the color array used to apply cyclic background colors to
/// category, group and item cards, and the default page size for item listings.
/// </summary>
public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    /// <summary>
    /// 21-color palette cycled across card backgrounds.
    /// Defaults to an empty array when not configured; callers should guard
    /// with a fallback such as <c>"#cccccc"</c>.
    /// </summary>
    public string[] Colors { get; set; } = Array.Empty<string>();

    /// <summary>Maximum items returned per page for item grids (default 50).</summary>
    public int ItemsPerPage { get; set; } = 50;

    /// <summary>
    /// Returns the color at position <paramref name="index"/> cycling cyclically
    /// through the <see cref="Colors"/> array. Returns a neutral grey when the
    /// array is empty.
    /// </summary>
    public string GetColor(int index) =>
        Colors.Length == 0 ? "#6c757d" : Colors[index % Colors.Length];
}
