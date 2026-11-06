using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PosWebApp.Infrastructure;
using PosWebApp.Infrastructure.Exceptions;
using PosWebApp.Models.DTOs;
using PosWebApp.Models.ViewModels;
using PosWebApp.Services;

namespace PosWebApp.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="SaleManagerService"/>.
/// All external dependencies are mocked; no network or database I/O.
/// </summary>
public class SaleManagerServiceTests
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static ISession BuildSession(CartState? state = null)
    {
        var session = new Mock<ISession>();
        var dict    = new Dictionary<string, byte[]>();

        session.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
               .Callback<string, byte[]>((k, v) => dict[k] = v);

        session.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
               .Returns((string key, out byte[]? value) =>
               {
                   var found = dict.TryGetValue(key, out value);
                   return found;
               });

        if (state is not null)
        {
            // Pre-populate the session with the given cart state.
            var session2 = session.Object;
            session2.SetObject(SessionKeys.Cart, state);
        }

        return session.Object;
    }

    private static CartState BuildCartWithItems() => new CartState(new List<CartItem>
    {
        new CartItem(
            ItemId:      1,
            ItemCode:    "ITEM001",
            ItemName:    "Test Product",
            ImageUrl:    "/img/item1.jpg",
            Quantity:    2,
            UnitPrice:   5.00m,
            PriceTierId: 1,
            Discount:    0m)
    });

    private static PagedResult<SaleListItemDto> EmptyPagedResult() =>
        new PagedResult<SaleListItemDto>(
            Items:      new List<SaleListItemDto>(),
            TotalCount: 0,
            Page:       1,
            PageSize:   20);

    // ─────────────────────────────────────────────────────────────────────────
    //  ProcessSaleAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessSaleAsync_EmptyCart_ThrowsCartValidationException()
    {
        // Arrange
        var session = BuildSession(CartState.Empty);

        var clientMock = new Mock<IServiceClient>();
        var cartMock   = new Mock<ICartService>();
        cartMock.Setup(c => c.GetCart(session)).Returns(CartState.Empty);

        var sut = new SaleManagerService(clientMock.Object, cartMock.Object,
                                         NullLogger<SaleManagerService>.Instance);

        var request = new ProcessSaleRequest(session, StaffUserID: 1,
                                             CustomerID: null, PaymentMethod: "Cash");

        // Act & Assert
        await Assert.ThrowsAsync<CartValidationException>(
            () => sut.ProcessSaleAsync(request));

        // Backend_Service must NOT be called when cart is empty.
        clientMock.Verify(c => c.CreateSaleAsync(It.IsAny<CreateSaleRequest>()), Times.Never);
    }

    [Fact]
    public async Task ProcessSaleAsync_NonEmptyCart_CallsCreateSaleAndClearsCart()
    {
        // Arrange
        var cartState  = BuildCartWithItems();
        var session    = BuildSession(cartState);

        var clientMock = new Mock<IServiceClient>();
        clientMock.Setup(c => c.CreateSaleAsync(It.IsAny<CreateSaleRequest>()))
                  .ReturnsAsync(new SaleCreationResult(SaleID: 42, InvoiceNo: "INV-0042"));

        var cartMock = new Mock<ICartService>();
        cartMock.Setup(c => c.GetCart(session)).Returns(cartState);
        cartMock.Setup(c => c.ClearCartAsync(session, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

        var sut = new SaleManagerService(clientMock.Object, cartMock.Object,
                                         NullLogger<SaleManagerService>.Instance);

        var request = new ProcessSaleRequest(session, StaffUserID: 7,
                                             CustomerID: 3, PaymentMethod: "Cash");

        // Act
        var result = await sut.ProcessSaleAsync(request);

        // Assert — correct result values
        Assert.Equal(42, result.SaleID);
        Assert.Equal("INV-0042", result.InvoiceNo);

        // Assert — Backend_Service called with correct staff and customer IDs
        clientMock.Verify(c => c.CreateSaleAsync(
            It.Is<CreateSaleRequest>(r =>
                r.StaffUserID   == 7   &&
                r.CustomerID    == 3   &&
                r.PaymentMethod == "Cash" &&
                r.Items.Count   == 1)),
            Times.Once);

        // Assert — cart cleared after success
        cartMock.Verify(c => c.ClearCartAsync(session, "7"), Times.Once);
    }

    [Fact]
    public async Task ProcessSaleAsync_BackendServiceFails_DoesNotClearCart()
    {
        // Arrange
        var cartState  = BuildCartWithItems();
        var session    = BuildSession(cartState);

        var clientMock = new Mock<IServiceClient>();
        clientMock.Setup(c => c.CreateSaleAsync(It.IsAny<CreateSaleRequest>()))
                  .ThrowsAsync(new SaleProcessingException("Backend rejected the sale."));

        var cartMock = new Mock<ICartService>();
        cartMock.Setup(c => c.GetCart(session)).Returns(cartState);

        var sut = new SaleManagerService(clientMock.Object, cartMock.Object,
                                         NullLogger<SaleManagerService>.Instance);

        var request = new ProcessSaleRequest(session, StaffUserID: 7,
                                             CustomerID: null, PaymentMethod: "Card");

        // Act & Assert
        await Assert.ThrowsAsync<SaleProcessingException>(
            () => sut.ProcessSaleAsync(request));

        // Cart must NOT be cleared on failure.
        cartMock.Verify(c => c.ClearCartAsync(It.IsAny<ISession>(), It.IsAny<string>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  GetSaleHistoryAsync — role-based StaffUserID filtering
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSaleHistoryAsync_StaffRole_PassesStaffUserIdToServiceClient()
    {
        // Arrange — Staff query has StaffUserID set to the staff member's own ID.
        var staffQuery = new SaleHistoryQuery(
            StaffUserID: 99,
            UserLevel:   "Staff",
            StartDate:   null,
            EndDate:     null,
            Page:        1,
            PageSize:    20);

        var clientMock = new Mock<IServiceClient>();
        SaleHistoryQuery? capturedQuery = null;
        clientMock.Setup(c => c.GetSaleHistoryAsync(It.IsAny<SaleHistoryQuery>()))
                  .Callback<SaleHistoryQuery>(q => capturedQuery = q)
                  .ReturnsAsync(EmptyPagedResult());

        var sut = new SaleManagerService(clientMock.Object, Mock.Of<ICartService>(),
                                         NullLogger<SaleManagerService>.Instance);

        // Act
        await sut.GetSaleHistoryAsync(staffQuery);

        // Assert — StaffUserID must be forwarded as-is
        Assert.NotNull(capturedQuery);
        Assert.Equal(99, capturedQuery!.StaffUserID);
        Assert.Equal("Staff", capturedQuery.UserLevel);
    }

    [Fact]
    public async Task GetSaleHistoryAsync_ManagerRole_PassesNullStaffUserIdToServiceClient()
    {
        // Arrange — Manager: caller may inadvertently supply a StaffUserID; the service
        // must null it out before forwarding.
        var managerQuery = new SaleHistoryQuery(
            StaffUserID: 99,   // should be nulled out for Manager
            UserLevel:   "Manager",
            StartDate:   null,
            EndDate:     null,
            Page:        1,
            PageSize:    20);

        var clientMock = new Mock<IServiceClient>();
        SaleHistoryQuery? capturedQuery = null;
        clientMock.Setup(c => c.GetSaleHistoryAsync(It.IsAny<SaleHistoryQuery>()))
                  .Callback<SaleHistoryQuery>(q => capturedQuery = q)
                  .ReturnsAsync(EmptyPagedResult());

        var sut = new SaleManagerService(clientMock.Object, Mock.Of<ICartService>(),
                                         NullLogger<SaleManagerService>.Instance);

        // Act
        await sut.GetSaleHistoryAsync(managerQuery);

        // Assert — StaffUserID must be null so Backend_Service returns all sales
        Assert.NotNull(capturedQuery);
        Assert.Null(capturedQuery!.StaffUserID);
    }

    [Fact]
    public async Task GetSaleHistoryAsync_AdminRole_PassesNullStaffUserIdToServiceClient()
    {
        // Arrange
        var adminQuery = new SaleHistoryQuery(
            StaffUserID: 5,   // should be nulled out for Admin
            UserLevel:   "Admin",
            StartDate:   null,
            EndDate:     null,
            Page:        1,
            PageSize:    20);

        var clientMock = new Mock<IServiceClient>();
        SaleHistoryQuery? capturedQuery = null;
        clientMock.Setup(c => c.GetSaleHistoryAsync(It.IsAny<SaleHistoryQuery>()))
                  .Callback<SaleHistoryQuery>(q => capturedQuery = q)
                  .ReturnsAsync(EmptyPagedResult());

        var sut = new SaleManagerService(clientMock.Object, Mock.Of<ICartService>(),
                                         NullLogger<SaleManagerService>.Instance);

        // Act
        await sut.GetSaleHistoryAsync(adminQuery);

        // Assert
        Assert.NotNull(capturedQuery);
        Assert.Null(capturedQuery!.StaffUserID);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  GetSaleDetailAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSaleDetailAsync_ForwardsSaleIdToServiceClient()
    {
        // Arrange
        var detail = new SaleDetailDto(
            SaleID:        42,
            InvoiceNo:     "INV-0042",
            CreatedDate:   DateTime.UtcNow,
            StaffUserName: "Alice",
            CustomerName:  null,
            Total:         10.00m,
            PaymentMethod: "Cash",
            Items:         new List<SaleItemDto>());

        var clientMock = new Mock<IServiceClient>();
        clientMock.Setup(c => c.GetSaleDetailAsync(42)).ReturnsAsync(detail);

        var sut = new SaleManagerService(clientMock.Object, Mock.Of<ICartService>(),
                                         NullLogger<SaleManagerService>.Instance);

        // Act
        var result = await sut.GetSaleDetailAsync(42);

        // Assert
        Assert.Equal(42, result.SaleID);
        clientMock.Verify(c => c.GetSaleDetailAsync(42), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  VoidSaleAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VoidSaleAsync_EmptyReason_ThrowsCartValidationException(string? reason)
    {
        // Arrange
        var clientMock = new Mock<IServiceClient>();
        var sut = new SaleManagerService(clientMock.Object, Mock.Of<ICartService>(),
                                         NullLogger<SaleManagerService>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<CartValidationException>(
            () => sut.VoidSaleAsync(1, reason!, staffUserId: 5));

        // Backend_Service must NOT be called
        clientMock.Verify(c => c.VoidSaleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()),
                          Times.Never);
    }

    [Fact]
    public async Task VoidSaleAsync_ValidReason_CallsServiceClientWithCorrectParameters()
    {
        // Arrange
        var clientMock = new Mock<IServiceClient>();
        clientMock.Setup(c => c.VoidSaleAsync(10, "Customer request", 7))
                  .ReturnsAsync(true);

        var sut = new SaleManagerService(clientMock.Object, Mock.Of<ICartService>(),
                                         NullLogger<SaleManagerService>.Instance);

        // Act
        var result = await sut.VoidSaleAsync(10, "Customer request", staffUserId: 7);

        // Assert
        Assert.True(result);
        clientMock.Verify(c => c.VoidSaleAsync(10, "Customer request", 7), Times.Once);
    }
}
