namespace PosWebApp.Models.ViewModels;

/// <summary>
/// Request payload for adding an item to the cart.
/// </summary>
public class AddToCartRequest
{
    /// <summary>The item's unique identifier.</summary>
    public int ItemId { get; set; }

    /// <summary>The item's display code.</summary>
    public string ItemCode { get; set; } = "";

    /// <summary>The item's display name.</summary>
    public string ItemName { get; set; } = "";

    /// <summary>URL of the item's image.</summary>
    public string ImageUrl { get; set; } = "";

    /// <summary>Number of units to add. Must be at least 1.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Optional unit price override (e.g. entered manually in the item detail modal).
    /// When greater than zero this price is used instead of the tier-resolved price.
    /// </summary>
    public double UnitPriceOverride { get; set; } = 0;
}
