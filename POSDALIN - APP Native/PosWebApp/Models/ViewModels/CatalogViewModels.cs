namespace PosWebApp.Models.ViewModels;

/// <summary>
/// View model for a single category card rendered on the home page.
/// </summary>
public class CategoryViewModel
{
    /// <summary>Primary key of the category.</summary>
    public int CategoryId { get; set; }

    /// <summary>Display name of the category.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// URL of the category image served by the Backend_Service image endpoint.
    /// </summary>
    public string ImageUrl { get; set; } = "";

    /// <summary>
    /// Background color assigned cyclically from <c>Catalog:Colors</c> in
    /// <c>appsettings.json</c>.  Hex color string, e.g. <c>#04478a</c>.
    /// </summary>
    public string BackgroundColor { get; set; } = "";

    /// <summary>
    /// <see langword="true"/> when the category contains groups;
    /// <see langword="false"/> when it links directly to items.
    /// </summary>
    public bool HasGroups { get; set; }
}

/// <summary>
/// View model for the home page (Index) — carries the category grid and
/// the Quick Sale link target.
/// </summary>
public class IndexViewModel
{
    /// <summary>
    /// Ordered list of category cards to display in the responsive grid.
    /// </summary>
    public IReadOnlyList<ItemCardViewModel> Categories { get; set; } =
        new List<ItemCardViewModel>();
}

/// <summary>
/// Identifies what kind of entity a card represents in the product grid.
/// </summary>
public enum ItemCardType
{
    /// <summary>A product category card (used on the home page).</summary>
    Category,
    /// <summary>A product group card (used on the Groups page).</summary>
    Group,
    /// <summary>A product item card (used on the Items page).</summary>
    Item,
}

/// <summary>
/// View model passed to <c>_ItemCardPartial.cshtml</c>.
/// Represents a single product/category/group card in the responsive grid.
/// </summary>
public class ItemCardViewModel
{
    /// <summary>Primary key of the entity (category, group, or item).</summary>
    public int Id { get; set; }

    /// <summary>Display label for the card (product, category, or group name).</summary>
    public string Name { get; set; } = "";

    /// <summary>Item code — only relevant for <see cref="ItemCardType.Item"/> cards.</summary>
    public string ItemCode { get; set; } = "";

    /// <summary>
    /// Source URL for the card image.  The partial falls back to
    /// <c>/images/placeholder.png</c> on error.
    /// </summary>
    public string ImageUrl { get; set; } = "";

    /// <summary>
    /// Override for the img <c>alt</c> attribute.
    /// When <see langword="null"/> or empty the <see cref="Name"/> is used.
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Navigation target — rendered as the <c>href</c> of the card anchor.
    /// Leave as <c>"#"</c> for item cards that open a modal instead of navigating.
    /// </summary>
    public string Href { get; set; } = "#";

    /// <summary>
    /// CSS colour value for the coloured header strip (e.g. <c>#04478a</c>).
    /// </summary>
    public string BackgroundColor { get; set; } = "#6c757d";

    /// <summary>
    /// Optional quantity already in the cart.  A badge is shown when &gt; 0.
    /// Only relevant for <see cref="ItemCardType.Item"/> cards.
    /// </summary>
    public int? CartQty { get; set; }

    /// <summary>Unit price — only relevant for <see cref="ItemCardType.Item"/> cards.</summary>
    public decimal Price { get; set; }

    /// <summary>What kind of entity this card represents.</summary>
    public ItemCardType CardType { get; set; } = ItemCardType.Item;
}

/// <summary>
/// View model for a single price tier displayed in the item detail modal.
/// </summary>
public class PriceTierViewModel
{
    /// <summary>Primary key of the price tier.</summary>
    public int Id { get; set; }

    /// <summary>Lower bound of the quantity range (inclusive).</summary>
    public int FromQty { get; set; }

    /// <summary>
    /// Upper bound of the quantity range (inclusive).
    /// 0 means "and above" (no upper bound).
    /// </summary>
    public int ToQty { get; set; }

    /// <summary>Price in USD for this tier.</summary>
    public decimal Price { get; set; }

    /// <summary>Pre-formatted USD string, e.g. <c>$12.50</c>.</summary>
    public string FormattedUsd { get; set; } = "";

    /// <summary>Pre-formatted KHR string, e.g. <c>51,250៛</c>.</summary>
    public string FormattedKhr { get; set; } = "";
}

/// <summary>
/// View model for the item detail Bootstrap modal.
/// Passed to <c>_ItemDetailModal.cshtml</c> via <c>ViewData</c> and as the
/// direct model when returned by <c>OnGetDetailAsync</c>.
///
/// Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10, 5.11
/// </summary>
public class ItemDetailViewModel
{
    /// <summary>Primary key of the item.</summary>
    public int ItemId { get; set; }

    /// <summary>Unique item code (e.g. SKU).</summary>
    public string ItemCode { get; set; } = "";

    /// <summary>Display name of the item.</summary>
    public string ItemName { get; set; } = "";

    /// <summary>Full-resolution image URL.</summary>
    public string ImageUrl { get; set; } = "";

    /// <summary>
    /// Responsive srcset value generated by <c>IImageOptimizerService.BuildSrcSet</c>.
    /// Empty string when images are not available.
    /// </summary>
    public string SrcSet { get; set; } = "";

    /// <summary>
    /// Price tiers for this item, ordered by <c>FromQty</c> ascending.
    /// </summary>
    public IReadOnlyList<PriceTierViewModel> PriceTiers { get; set; } =
        new List<PriceTierViewModel>();

    /// <summary>
    /// Current quantity of this item already in the customer's cart.
    /// 0 when the item is not yet in the cart.
    /// </summary>
    public int CurrentCartQuantity { get; set; }
}
