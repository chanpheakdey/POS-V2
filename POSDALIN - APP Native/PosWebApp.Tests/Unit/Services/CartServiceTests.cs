using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Moq;
using PosWebApp.Hubs;
using PosWebApp.Infrastructure;
using PosWebApp.Models.DTOs;
using PosWebApp.Models.ViewModels;
using PosWebApp.Services;

namespace PosWebApp.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="CartService"/>. These tests focus on:
/// <list type="bullet">
///   <item>The pure <see cref="ICartService.AddItemPure"/> logic (no I/O),</item>
///   <item>Session persistence via <see cref="SessionExtensions"/>,</item>
///   <item>SignalR notifications on cart mutations.</item>
/// </list>
/// </summary>
public class CartServiceTests
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Pure add-item logic (ICartService.AddItemPure)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddItemPure_AddsNewItemWithCorrectTier()
    {
        // Arrange: empty cart, single tier
        var state = CartState.Empty;
        var request = new AddToCartRequest
        {
            ItemId   = 100,
            ItemCode = "ITEM100",
            ItemName = "Test Item",
            ImageUrl = "/img/test.jpg",
            Quantity = 5,
        };
        var tiers = new List<PriceTierDto>
        {
            new(ID: 1, ItemID: 100, FromQty: 1, ToQty: 0, Price: 10.00m, Currency: "USD")
        };

        // Act
        var updated = ICartService.AddItemPure(state, request, tiers);

        // Assert
        Assert.Single(updated.Items);
        var item = updated.Items[0];
        Assert.Equal(100, item.ItemId);
        Assert.Equal("ITEM100", item.ItemCode);
        Assert.Equal("Test Item", item.ItemName);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(10.00m, item.UnitPrice);
        Assert.Equal(1, item.PriceTierId);
        Assert.Equal(50.00m, item.LineTotal);
    }

    [Fact]
    public void AddItemPure_AccumulatesQuantity_WhenItemExists()
    {
        // Arrange: cart with 1 unit of item 100 at $10
        var existing = new CartItem(
            ItemId: 100, ItemCode: "ITEM100", ItemName: "Test Item",
            ImageUrl: "/img/test.jpg", Quantity: 1, UnitPrice: 10.00m, PriceTierId: 1);
        var state = new CartState(new List<CartItem> { existing });

        var request = new AddToCartRequest
        {
            ItemId = 100,
            ItemCode = "ITEM100",
            ItemName = "Test Item",
            ImageUrl = "/img/test.jpg",
            Quantity = 2,
        };
        var tiers = new List<PriceTierDto>
        {
            new(ID: 1, ItemID: 100, FromQty: 1, ToQty: 0, Price: 10.00m, Currency: "USD")
        };

        // Act
        var updated = ICartService.AddItemPure(state, request, tiers);

        // Assert
        Assert.Single(updated.Items);
        var item = updated.Items[0];
        Assert.Equal(100, item.ItemId);
        Assert.Equal(3, item.Quantity); // 1 + 2
        Assert.Equal(10.00m, item.UnitPrice);
        Assert.Equal(30.00m, item.LineTotal);
    }

    [Fact]
    public void AddItemPure_UpdatesPriceWhenQuantityCrossesTier()
    {
        // Arrange: cart with 5 units at tier 1 ($10), add 6 more to trigger tier 2 ($8)
        var existing = new CartItem(
            ItemId: 100, ItemCode: "ITEM100", ItemName: "Test Item",
            ImageUrl: "/img/test.jpg", Quantity: 5, UnitPrice: 10.00m, PriceTierId: 1);
        var state = new CartState(new List<CartItem> { existing });

        var request = new AddToCartRequest
        {
            ItemId = 100,
            ItemCode = "ITEM100",
            ItemName = "Test Item",
            ImageUrl = "/img/test.jpg",
            Quantity = 6, // total will be 11 → tier 2
        };
        var tiers = new List<PriceTierDto>
        {
            new(ID: 1, ItemID: 100, FromQty: 1,  ToQty: 10, Price: 10.00m, Currency: "USD"),
            new(ID: 2, ItemID: 100, FromQty: 11, ToQty: 0,  Price: 8.00m,  Currency: "USD")
        };

        // Act
        var updated = ICartService.AddItemPure(state, request, tiers);

        // Assert
        Assert.Single(updated.Items);
        var item = updated.Items[0];
        Assert.Equal(11, item.Quantity);
        Assert.Equal(8.00m, item.UnitPrice); // tier 2 unit price
        Assert.Equal(2, item.PriceTierId);
        Assert.Equal(88.00m, item.LineTotal); // 11 * 8
    }

    [Fact]
    public void AddItemPure_ThrowsWhenTiersEmpty()
    {
        // Arrange
        var state = CartState.Empty;
        var request = new AddToCartRequest
        {
            ItemId = 100,
            ItemCode = "ITEM100",
            ItemName = "Test Item",
            ImageUrl = "/img/test.jpg",
            Quantity = 1,
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ICartService.AddItemPure(state, request, new List<PriceTierDto>()));
        Assert.Contains("Tier list must not be empty", ex.Message);
    }

    [Fact]
    public void AddItemPure_ThrowsWhenQuantityIsZero()
    {
        // Arrange
        var state = CartState.Empty;
        var request = new AddToCartRequest
        {
            ItemId = 100,
            ItemCode = "ITEM100",
            ItemName = "Test Item",
            ImageUrl = "/img/test.jpg",
            Quantity = 0,
        };
        var tiers = new List<PriceTierDto>
        {
            new(ID: 1, ItemID: 100, FromQty: 1, ToQty: 0, Price: 10.00m, Currency: "USD")
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ICartService.AddItemPure(state, request, tiers));
        Assert.Contains("Quantity must be greater than zero", ex.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Session-backed operations
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddItemAsync_PersistsToSessionAndNotifiesHub()
    {
        // Arrange
        var session = new FakeSession();
        var mockHub = new Mock<IHubContext<CartHub>>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var service = new CartService(mockHub.Object);

        var request = new AddToCartRequest
        {
            ItemId = 100,
            ItemCode = "ITEM100",
            ItemName = "Test Item",
            ImageUrl = "/img/test.jpg",
            Quantity = 3,
        };
        var tiers = new List<PriceTierDto>
        {
            new(ID: 1, ItemID: 100, FromQty: 1, ToQty: 0, Price: 10.00m, Currency: "USD")
        };

        // Act
        var result = await service.AddItemAsync(session, request, tiers, "user123");

        // Assert: item returned
        Assert.Equal(100, result.ItemId);
        Assert.Equal(3, result.Quantity);
        Assert.Equal(10.00m, result.UnitPrice);

        // Assert: cart persisted to session
        var cart = service.GetCart(session);
        Assert.Single(cart.Items);
        Assert.Equal(3, cart.ItemCount);
        Assert.Equal(30.00m, cart.Total);

        // Assert: SignalR hub notified with correct count
        mockClientProxy.Verify(c => c.SendCoreAsync(
            "CartUpdated",
            It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 3),
            default), Times.Once);
    }

    [Fact]
    public async Task UpdateQuantityAsync_UpdatesTierWhenCrossingBoundary()
    {
        // Arrange: cart with 5 units at tier 1 ($10)
        var session = new FakeSession();
        var existing = new CartItem(
            ItemId: 100, ItemCode: "ITEM100", ItemName: "Test Item",
            ImageUrl: "/img/test.jpg", Quantity: 5, UnitPrice: 10.00m, PriceTierId: 1);
        session.SetObject("Current_Cart", new CartState(new List<CartItem> { existing }));

        var mockHub = new Mock<IHubContext<CartHub>>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var service = new CartService(mockHub.Object);

        var tiers = new List<PriceTierDto>
        {
            new(ID: 1, ItemID: 100, FromQty: 1,  ToQty: 10, Price: 10.00m, Currency: "USD"),
            new(ID: 2, ItemID: 100, FromQty: 11, ToQty: 0,  Price: 8.00m,  Currency: "USD")
        };

        // Act: update quantity to 11 (crosses into tier 2)
        var result = await service.UpdateQuantityAsync(session, 100, 11, tiers, "user123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(11, result.Quantity);
        Assert.Equal(8.00m, result.UnitPrice); // tier 2 price
        Assert.Equal(2, result.PriceTierId);

        // Assert: SignalR notified
        mockClientProxy.Verify(c => c.SendCoreAsync(
            "CartUpdated",
            It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 11),
            default), Times.Once);
    }

    [Fact]
    public async Task UpdateQuantityAsync_RemovesItemWhenQuantityIsZero()
    {
        // Arrange
        var session = new FakeSession();
        var existing = new CartItem(
            ItemId: 100, ItemCode: "ITEM100", ItemName: "Test Item",
            ImageUrl: "/img/test.jpg", Quantity: 5, UnitPrice: 10.00m, PriceTierId: 1);
        session.SetObject("Current_Cart", new CartState(new List<CartItem> { existing }));

        var mockHub = new Mock<IHubContext<CartHub>>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var service = new CartService(mockHub.Object);
        var tiers = new List<PriceTierDto>
        {
            new(ID: 1, ItemID: 100, FromQty: 1, ToQty: 0, Price: 10.00m, Currency: "USD")
        };

        // Act
        var result = await service.UpdateQuantityAsync(session, 100, 0, tiers, "user123");

        // Assert: returns null when removed
        Assert.Null(result);

        // Assert: cart is now empty
        var cart = service.GetCart(session);
        Assert.Empty(cart.Items);
        Assert.Equal(0, cart.ItemCount);

        // Assert: SignalR notified with count 0
        mockClientProxy.Verify(c => c.SendCoreAsync(
            "CartUpdated",
            It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 0),
            default), Times.Once);
    }

    [Fact]
    public async Task RemoveItemAsync_RemovesItemFromSession()
    {
        // Arrange: cart with 2 items
        var session = new FakeSession();
        var items = new List<CartItem>
        {
            new(ItemId: 100, ItemCode: "A", ItemName: "Item A", ImageUrl: "/a", Quantity: 2, UnitPrice: 10m, PriceTierId: 1),
            new(ItemId: 200, ItemCode: "B", ItemName: "Item B", ImageUrl: "/b", Quantity: 3, UnitPrice: 5m,  PriceTierId: 2)
        };
        session.SetObject("Current_Cart", new CartState(items));

        var mockHub = new Mock<IHubContext<CartHub>>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var service = new CartService(mockHub.Object);

        // Act: remove item 100
        await service.RemoveItemAsync(session, 100, "user123");

        // Assert: only item 200 remains
        var cart = service.GetCart(session);
        Assert.Single(cart.Items);
        Assert.Equal(200, cart.Items[0].ItemId);
        Assert.Equal(3, cart.ItemCount);

        // Assert: SignalR notified
        mockClientProxy.Verify(c => c.SendCoreAsync(
            "CartUpdated",
            It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 3),
            default), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Edge cases (Requirements: 6.7)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Removing an item that is not in the cart must be a no-op:
    /// the cart remains unchanged and SignalR is still notified with
    /// the current (unchanged) item count.
    /// </summary>
    [Fact]
    public async Task RemoveItemAsync_NonExistentItem_IsNoOp()
    {
        // Arrange: cart with one item (id 100)
        var session = new FakeSession();
        var items = new List<CartItem>
        {
            new(ItemId: 100, ItemCode: "A", ItemName: "Item A", ImageUrl: "/a",
                Quantity: 2, UnitPrice: 10m, PriceTierId: 1)
        };
        session.SetObject("Current_Cart", new CartState(items));

        var mockHub = new Mock<IHubContext<CartHub>>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var service = new CartService(mockHub.Object);

        // Act: attempt to remove item 999, which does not exist in the cart
        await service.RemoveItemAsync(session, 999, "user123");

        // Assert: cart is unchanged — item 100 is still present
        var cart = service.GetCart(session);
        Assert.Single(cart.Items);
        Assert.Equal(100, cart.Items[0].ItemId);
        Assert.Equal(2, cart.ItemCount);

        // Assert: SignalR is still notified (with unchanged count)
        mockClientProxy.Verify(c => c.SendCoreAsync(
            "CartUpdated",
            It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 2),
            default), Times.Once);
    }

    /// <summary>
    /// <see cref="CartService.ClearCartAsync"/> must always yield an empty
    /// <see cref="CartState"/> regardless of what was in the cart beforehand.
    /// Verifies Items is empty, ItemCount is 0, and Total is 0.
    /// </summary>
    [Fact]
    public async Task ClearCartAsync_ProducesEmptyCartState()
    {
        // Arrange: cart with several items
        var session = new FakeSession();
        var items = new List<CartItem>
        {
            new(ItemId: 1, ItemCode: "X1", ItemName: "Item X1", ImageUrl: "/x1",
                Quantity: 4, UnitPrice: 7.50m, PriceTierId: 1),
            new(ItemId: 2, ItemCode: "X2", ItemName: "Item X2", ImageUrl: "/x2",
                Quantity: 1, UnitPrice: 25.00m, PriceTierId: 1)
        };
        session.SetObject("Current_Cart", new CartState(items));

        var mockHub = new Mock<IHubContext<CartHub>>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var service = new CartService(mockHub.Object);

        // Act
        await service.ClearCartAsync(session, "user123");

        // Assert: resulting CartState is structurally empty
        var cart = service.GetCart(session);
        Assert.Empty(cart.Items);
        Assert.Equal(0, cart.ItemCount);
        Assert.Equal(0m, cart.Total);

        // Assert: SignalR notified with count 0
        mockClientProxy.Verify(c => c.SendCoreAsync(
            "CartUpdated",
            It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 0),
            default), Times.Once);
    }

    /// <summary>
    /// Updating the quantity of an item to zero must remove it from the cart,
    /// leaving the remaining items untouched.
    /// </summary>
    [Fact]
    public async Task UpdateQuantityAsync_ToZero_RemovesItem()
    {
        // Arrange: two items in cart
        var session = new FakeSession();
        var items = new List<CartItem>
        {
            new(ItemId: 100, ItemCode: "A", ItemName: "Item A", ImageUrl: "/a",
                Quantity: 3, UnitPrice: 10m, PriceTierId: 1),
            new(ItemId: 200, ItemCode: "B", ItemName: "Item B", ImageUrl: "/b",
                Quantity: 2, UnitPrice: 5m,  PriceTierId: 1)
        };
        session.SetObject("Current_Cart", new CartState(items));

        var mockHub = new Mock<IHubContext<CartHub>>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var service = new CartService(mockHub.Object);
        var tiers = new List<PriceTierDto>
        {
            new(ID: 1, ItemID: 100, FromQty: 1, ToQty: 0, Price: 10.00m, Currency: "USD")
        };

        // Act: set quantity of item 100 to 0
        var result = await service.UpdateQuantityAsync(session, 100, 0, tiers, "user123");

        // Assert: returns null (item was removed, not updated)
        Assert.Null(result);

        // Assert: only item 200 remains
        var cart = service.GetCart(session);
        Assert.Single(cart.Items);
        Assert.Equal(200, cart.Items[0].ItemId);
        Assert.Equal(2, cart.ItemCount);

        // Assert: SignalR notified with updated count (2)
        mockClientProxy.Verify(c => c.SendCoreAsync(
            "CartUpdated",
            It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 2),
            default), Times.Once);
    }

    [Fact]
    public async Task ClearCartAsync_RemovesAllItems()
    {
        // Arrange: cart with items
        var session = new FakeSession();
        var items = new List<CartItem>
        {
            new(ItemId: 100, ItemCode: "A", ItemName: "Item A", ImageUrl: "/a", Quantity: 2, UnitPrice: 10m, PriceTierId: 1),
            new(ItemId: 200, ItemCode: "B", ItemName: "Item B", ImageUrl: "/b", Quantity: 3, UnitPrice: 5m,  PriceTierId: 2)
        };
        session.SetObject("Current_Cart", new CartState(items));

        var mockHub = new Mock<IHubContext<CartHub>>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var service = new CartService(mockHub.Object);

        // Act
        await service.ClearCartAsync(session, "user123");

        // Assert: cart is empty
        var cart = service.GetCart(session);
        Assert.Empty(cart.Items);
        Assert.Equal(0, cart.ItemCount);
        Assert.Equal(0m, cart.Total);

        // Assert: SignalR notified with count 0
        mockClientProxy.Verify(c => c.SendCoreAsync(
            "CartUpdated",
            It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 0),
            default), Times.Once);
    }

    [Fact]
    public void CalculateTotal_ReturnsCorrectSum()
    {
        // Arrange
        var session = new FakeSession();
        var items = new List<CartItem>
        {
            new(ItemId: 100, ItemCode: "A", ItemName: "Item A", ImageUrl: "/a", Quantity: 2, UnitPrice: 10m, PriceTierId: 1),
            new(ItemId: 200, ItemCode: "B", ItemName: "Item B", ImageUrl: "/b", Quantity: 3, UnitPrice: 5m,  PriceTierId: 2)
        };
        session.SetObject("Current_Cart", new CartState(items));

        var mockHub = new Mock<IHubContext<CartHub>>();
        var service = new CartService(mockHub.Object);

        // Act
        var total = service.CalculateTotal(session);

        // Assert: 2*10 + 3*5 = 35
        Assert.Equal(35m, total);
    }

    [Fact]
    public void GetItemCount_ReturnsTotalQuantity()
    {
        // Arrange
        var session = new FakeSession();
        var items = new List<CartItem>
        {
            new(ItemId: 100, ItemCode: "A", ItemName: "Item A", ImageUrl: "/a", Quantity: 2, UnitPrice: 10m, PriceTierId: 1),
            new(ItemId: 200, ItemCode: "B", ItemName: "Item B", ImageUrl: "/b", Quantity: 3, UnitPrice: 5m,  PriceTierId: 2)
        };
        session.SetObject("Current_Cart", new CartState(items));

        var mockHub = new Mock<IHubContext<CartHub>>();
        var service = new CartService(mockHub.Object);

        // Act
        var count = service.GetItemCount(session);

        // Assert: 2 + 3 = 5
        Assert.Equal(5, count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Fake ISession for testing (in-memory dictionary)
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }
}
