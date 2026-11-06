using PosWebApp.Models.DTOs;
using PosWebApp.Models.ViewModels;

namespace PosWebApp.Services;

/// <summary>
/// Manages the session-based shopping cart for the currently served customer.
/// All mutating methods persist the updated <see cref="CartState"/> back to session
/// and broadcast a SignalR notification to the authenticated user's connections.
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Returns the current <see cref="CartState"/> from session, or an empty cart
    /// if no cart data is stored yet.
    /// </summary>
    CartState GetCart(ISession session);

    /// <summary>
    /// Adds the requested item to the cart (or increments its quantity if it is already
    /// present). The unit price is resolved via <c>PriceTierResolver.Resolve</c> against
    /// the supplied <paramref name="tiers"/>.
    /// </summary>
    /// <returns>The updated or newly inserted <see cref="CartItem"/>.</returns>
    Task<CartItem> AddItemAsync(ISession session, AddToCartRequest request,
        IReadOnlyList<PriceTierDto> tiers, string userId);

    /// <summary>
    /// Sets the quantity of <paramref name="itemId"/> to <paramref name="quantity"/>.
    /// Re-resolves the unit price against the supplied <paramref name="tiers"/>
    /// in case the new quantity crosses a tier boundary.
    /// If <paramref name="quantity"/> is zero or negative the item is removed.
    /// </summary>
    /// <returns>The updated <see cref="CartItem"/>, or <see langword="null"/> if it was removed.</returns>
    Task<CartItem?> UpdateQuantityAsync(ISession session, int itemId, int quantity,
        IReadOnlyList<PriceTierDto> tiers, string userId);

    /// <summary>Removes the item identified by <paramref name="itemId"/> from the cart.</summary>
    Task RemoveItemAsync(ISession session, int itemId, string userId);

    /// <summary>Removes all items from the cart.</summary>
    Task ClearCartAsync(ISession session, string userId);

    /// <summary>Returns the grand total of all line items in the current cart.</summary>
    decimal CalculateTotal(ISession session);

    /// <summary>Returns the total number of individual units across all cart entries.</summary>
    int GetItemCount(ISession session);

    /// <summary>
    /// Applies a discount to a specific cart item.
    /// Discount type may be "Percentage" (0–100) or "Fixed" (USD amount deducted per unit).
    /// For discounts > 20% a manager/admin authorisation UserID must be supplied.
    /// </summary>
    /// <param name="session">The current HTTP session.</param>
    /// <param name="itemId">The cart item to discount.</param>
    /// <param name="discountPercent">Percentage discount 0–100 (requirement 28.3).</param>
    /// <param name="discountType">Either "Percentage" or "Fixed" (requirement 28.4).</param>
    /// <param name="authorizingUserId">UserID of the manager/admin who approved discounts > 20% (requirement 28.4).</param>
    CartState ApplyDiscount(ISession session, int itemId, decimal discountPercent,
        string discountType, int authorizingUserId);

    /// <summary>
    /// Pure (no I/O) version of the discount logic intended for unit testing.
    /// Validates the range and returns the updated <see cref="CartState"/>.
    /// </summary>
    static CartState ApplyDiscountPure(CartState state, int itemId, decimal discountPercent,
        string discountType, int authorizingUserId)
        => CartService.ApplyDiscountPureImpl(state, itemId, discountPercent, discountType, authorizingUserId);

    /// <summary>
    /// Pure (no I/O) version of the add-item logic intended for unit testing.
    /// Resolves the correct price tier, increments the quantity if the item already
    /// exists, otherwise appends a new entry.
    /// </summary>
    static CartState AddItemPure(CartState state, AddToCartRequest request,
        IReadOnlyList<PriceTierDto> tiers)
        => CartService.AddItemPureImpl(state, request, tiers);
}
