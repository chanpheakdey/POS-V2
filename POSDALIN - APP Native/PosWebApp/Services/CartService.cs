using Microsoft.AspNetCore.SignalR;
using PosWebApp.Hubs;
using PosWebApp.Infrastructure;
using PosWebApp.Models.DTOs;
using PosWebApp.Models.ViewModels;

namespace PosWebApp.Services;

/// <summary>
/// Session-backed cart service.  All mutations:
/// <list type="bullet">
///   <item>read the current <see cref="CartState"/> from session via <see cref="SessionExtensions"/>,</item>
///   <item>apply the change using the pure helper <see cref="AddItemPureImpl"/>,</item>
///   <item>persist the updated state back to session, and</item>
///   <item>broadcast a <c>CartUpdated</c> SignalR message to the calling user.</item>
/// </list>
/// </summary>
public sealed class CartService : ICartService
{
    // Use the central key constant so the navbar partial and all callers read
    // from the same session slot (SessionKeys.Cart = "Current_Cart").
    private const string SessionKey = SessionKeys.Cart;

    private readonly IHubContext<CartHub> _hubContext;

    public CartService(IHubContext<CartHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Read-only helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public CartState GetCart(ISession session)
    {
        return session.GetObject<CartState>(SessionKey) ?? CartState.Empty;
    }

    /// <inheritdoc/>
    public double CalculateTotal(ISession session)
    {
        return GetCart(session).Total;
    }

    /// <inheritdoc/>
    public int GetItemCount(ISession session)
    {
        return GetCart(session).ItemCount;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Mutating operations
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<CartItem> AddItemAsync(
        ISession session,
        AddToCartRequest request,
        IReadOnlyList<PriceTierDto> tiers,
        string userId)
    {
        var current = GetCart(session);
        var updated = AddItemPureImpl(current, request, tiers);
        Save(session, updated);

        await NotifyAsync(userId, updated.ItemCount);

        // Return the updated cart entry for this item.
        return updated.Items.First(i => i.ItemId == request.ItemId);
    }

    /// <inheritdoc/>
    public async Task<CartItem?> UpdateQuantityAsync(
        ISession session,
        int itemId,
        int quantity,
        IReadOnlyList<PriceTierDto> tiers,
        string userId)
    {
        var current = GetCart(session);
        var items = new List<CartItem>(current.Items);

        var idx = items.FindIndex(i => i.ItemId == itemId);
        if (idx < 0)
            return null; // item not in cart — nothing to update

        CartItem? result;

        if (quantity <= 0)
        {
            // Treat zero/negative quantity as a remove.
            items.RemoveAt(idx);
            result = null;
        }
        else
        {
            var existing = items[idx];
            var tier = PriceTierResolver.Resolve(quantity, tiers);
            var updated = existing with
            {
                Quantity    = quantity,
                UnitPrice   = tier.Price,
                PriceTierId = tier.ID,
            };
            items[idx] = updated;
            result = updated;
        }

        var newState = new CartState(items);
        Save(session, newState);
        await NotifyAsync(userId, newState.ItemCount);

        return result;
    }

    /// <inheritdoc/>
    public async Task RemoveItemAsync(ISession session, int itemId, string userId)
    {
        var current = GetCart(session);
        var items = current.Items.Where(i => i.ItemId != itemId).ToList();
        var newState = new CartState(items);
        Save(session, newState);
        await NotifyAsync(userId, newState.ItemCount);
    }

    /// <inheritdoc/>
    public async Task ClearCartAsync(ISession session, string userId)
    {
        Save(session, CartState.Empty);
        await NotifyAsync(userId, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Discount
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public CartState ApplyDiscount(
        ISession session,
        int itemId,
        double discountPercent,
        string discountType,
        int authorizingUserId)
    {
        var current = GetCart(session);
        var updated = ApplyDiscountPureImpl(current, itemId, discountPercent, discountType, authorizingUserId);
        Save(session, updated);
        return updated;
    }

    /// <summary>
    /// Pure discount logic with no I/O side-effects.
    /// Validates 0–100 range, stores discount type, amount and authorising UserID
    /// on the <see cref="CartItem"/>.
    ///
    /// Requirements: 28.3, 28.4, 28.5
    /// </summary>
    internal static CartState ApplyDiscountPureImpl(
        CartState state,
        int itemId,
        double discountPercent,
        string discountType,
        int authorizingUserId)
    {
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent),
                "Discount must be between 0 and 100 percent.");

        if (string.IsNullOrWhiteSpace(discountType))
            throw new ArgumentException("Discount type must not be empty.", nameof(discountType));

        var items = new List<CartItem>(state.Items);
        var idx   = items.FindIndex(i => i.ItemId == itemId);
        if (idx < 0)
            throw new InvalidOperationException($"Item {itemId} is not in the cart.");

        var existing = items[idx];
        items[idx] = existing with
        {
            Discount             = discountPercent,
            DiscountType         = discountType,
            AuthorizingUserId    = authorizingUserId,
        };

        return new CartState(items);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Pure implementation (no I/O) — exposed for ICartService static method
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pure add-item logic with no side effects.  Used by both
    /// <see cref="AddItemAsync"/> and <see cref="ICartService.AddItemPure"/>.
    /// </summary>
    internal static CartState AddItemPureImpl(
        CartState state,
        AddToCartRequest request,
        IReadOnlyList<PriceTierDto> tiers)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (tiers is null || tiers.Count == 0)
            throw new ArgumentException("Tier list must not be empty.", nameof(tiers));
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(request));

        var items = new List<CartItem>(state.Items);
        var idx   = items.FindIndex(i => i.ItemId == request.ItemId);

        if (idx >= 0)
        {
            // Item exists — accumulate quantity and re-resolve price.
            var existing = items[idx];
            var newQty   = existing.Quantity + request.Quantity;
            double unitPrice;
            int tierId;
            if (request.UnitPriceOverride > 0)
            {
                // Manual fee override — keep the override price, tier ID unchanged
                unitPrice = request.UnitPriceOverride;
                tierId    = existing.PriceTierId;
            }
            else
            {
                var tier  = PriceTierResolver.Resolve(newQty, tiers);
                unitPrice = tier.Price;
                tierId    = tier.ID;
            }
            items[idx] = existing with
            {
                Quantity    = newQty,
                UnitPrice   = unitPrice,
                PriceTierId = tierId,
            };
        }
        else
        {
            // New item — resolve price tier or use override.
            double unitPrice;
            int tierId;
            if (request.UnitPriceOverride > 0)
            {
                unitPrice = request.UnitPriceOverride;
                tierId    = 0; // no tier selected
            }
            else
            {
                var tier  = PriceTierResolver.Resolve(request.Quantity, tiers);
                unitPrice = tier.Price;
                tierId    = tier.ID;
            }
            items.Add(new CartItem(
                ItemId:      request.ItemId,
                ItemCode:    request.ItemCode,
                ItemName:    request.ItemName,
                ImageUrl:    request.ImageUrl,
                Quantity:    request.Quantity,
                UnitPrice:   unitPrice,
                PriceTierId: tierId));
        }

        return new CartState(items);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static void Save(ISession session, CartState state)
        => session.SetObject(SessionKey, state);

    private Task NotifyAsync(string userId, int itemCount)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.CompletedTask;

        return _hubContext.Clients
                          .User(userId)
                          .SendAsync("CartUpdated", itemCount);
    }
}
