using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PosWebApp.Models.DTOs;
using PosWebApp.Services;

namespace PosWebApp.Tests.Unit.Properties;

/// <summary>
/// Property-based tests for role-based sale history visibility in
/// <see cref="SaleManagerService.GetSaleHistoryAsync"/>.
///
/// **Property 7: Role-Based Sale History Visibility**
///
/// **Validates: Requirements 9.1, 9.2, 9.3**
/// </summary>
public class SalePropertyTests
{
    // ── Generators ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a positive staff user ID (1 – 100 000).
    /// </summary>
    public static Arbitrary<int> PositiveStaffUserIdArb() =>
        Gen.Choose(1, 100_000).ToArbitrary();

    /// <summary>
    /// Generates one of the two manager-level role strings recognised by the service.
    /// </summary>
    public static Arbitrary<string> ManagerOrAdminRoleArb() =>
        Gen.Elements("Manager", "Admin").ToArbitrary();

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static PagedResult<SaleListItemDto> EmptyPagedResult() =>
        new PagedResult<SaleListItemDto>(
            Items:      new List<SaleListItemDto>(),
            TotalCount: 0,
            Page:       1,
            PageSize:   20);

    /// <summary>
    /// Builds a <see cref="SaleManagerService"/> whose underlying
    /// <see cref="IServiceClient"/> captures the <see cref="SaleHistoryQuery"/>
    /// it receives and returns an empty paged result.
    /// </summary>
    private static (SaleManagerService Sut, Func<SaleHistoryQuery?> GetCaptured)
        BuildSutWithCapture()
    {
        SaleHistoryQuery? captured = null;

        var clientMock = new Mock<IServiceClient>();
        clientMock
            .Setup(c => c.GetSaleHistoryAsync(It.IsAny<SaleHistoryQuery>()))
            .Callback<SaleHistoryQuery>(q => captured = q)
            .ReturnsAsync(EmptyPagedResult());

        var sut = new SaleManagerService(
            clientMock.Object,
            Mock.Of<ICartService>(),
            NullLogger<SaleManagerService>.Instance);

        return (sut, () => captured);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// **Property 7a: Staff role preserves StaffUserID**
    ///
    /// For any positive <paramref name="staffUserId"/>, when
    /// <c>query.UserLevel == "Staff"</c>, the <see cref="SaleHistoryQuery"/> forwarded
    /// to <see cref="IServiceClient.GetSaleHistoryAsync"/> SHALL have
    /// <c>StaffUserID == staffUserId</c>.
    ///
    /// **Validates: Requirements 9.1, 9.2, 9.3**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(SalePropertyTests) }, MaxTest = 500)]
    public Property Property7a_StaffRole_StaffUserIdIsPreserved(int staffUserId)
    {
        var (sut, getCaptured) = BuildSutWithCapture();

        var query = new SaleHistoryQuery(
            StaffUserID: staffUserId,
            UserLevel:   "Staff",
            StartDate:   null,
            EndDate:     null,
            Page:        1,
            PageSize:    20);

        sut.GetSaleHistoryAsync(query).GetAwaiter().GetResult();

        var captured = getCaptured();

        bool queryWasCaptured  = captured is not null;
        bool staffIdPreserved  = captured?.StaffUserID == staffUserId;
        bool userLevelRetained = captured?.UserLevel == "Staff";

        return (queryWasCaptured && staffIdPreserved && userLevelRetained).Label(
            $"StaffUserID should be preserved for Staff role: " +
            $"input={staffUserId}, " +
            $"captured.StaffUserID={captured?.StaffUserID?.ToString() ?? "null"}, " +
            $"captured.UserLevel={captured?.UserLevel ?? "null"}");
    }

    /// <summary>
    /// **Property 7b: Manager/Admin role nulls out StaffUserID**
    ///
    /// For any positive <paramref name="staffUserId"/> and any role in {"Manager","Admin"},
    /// the <see cref="SaleHistoryQuery"/> forwarded to
    /// <see cref="IServiceClient.GetSaleHistoryAsync"/> SHALL have
    /// <c>StaffUserID == null</c>.
    ///
    /// **Validates: Requirements 9.1, 9.2, 9.3**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(SalePropertyTests) }, MaxTest = 500)]
    public Property Property7b_ManagerOrAdminRole_StaffUserIdIsNull(
        int staffUserId, string userLevel)
    {
        var (sut, getCaptured) = BuildSutWithCapture();

        var query = new SaleHistoryQuery(
            StaffUserID: staffUserId,
            UserLevel:   userLevel,
            StartDate:   null,
            EndDate:     null,
            Page:        1,
            PageSize:    20);

        sut.GetSaleHistoryAsync(query).GetAwaiter().GetResult();

        var captured = getCaptured();

        bool queryWasCaptured = captured is not null;
        bool staffIdIsNull    = captured?.StaffUserID is null;

        return (queryWasCaptured && staffIdIsNull).Label(
            $"StaffUserID should be null for {userLevel} role: " +
            $"input={staffUserId}, " +
            $"captured.StaffUserID={captured?.StaffUserID?.ToString() ?? "null"}");
    }

    // ── Example-based edge cases ──────────────────────────────────────────────

    [Fact]
    public async Task GetSaleHistoryAsync_StaffRole_ForwardsStaffUserIdUnchanged()
    {
        var (sut, getCaptured) = BuildSutWithCapture();

        var query = new SaleHistoryQuery(
            StaffUserID: 42,
            UserLevel:   "Staff",
            StartDate:   null,
            EndDate:     null,
            Page:        1,
            PageSize:    20);

        await sut.GetSaleHistoryAsync(query);

        var captured = getCaptured();
        Assert.NotNull(captured);
        Assert.Equal(42, captured!.StaffUserID);
    }

    [Fact]
    public async Task GetSaleHistoryAsync_ManagerRole_NullsOutStaffUserId()
    {
        var (sut, getCaptured) = BuildSutWithCapture();

        var query = new SaleHistoryQuery(
            StaffUserID: 99,
            UserLevel:   "Manager",
            StartDate:   null,
            EndDate:     null,
            Page:        1,
            PageSize:    20);

        await sut.GetSaleHistoryAsync(query);

        var captured = getCaptured();
        Assert.NotNull(captured);
        Assert.Null(captured!.StaffUserID);
    }

    [Fact]
    public async Task GetSaleHistoryAsync_AdminRole_NullsOutStaffUserId()
    {
        var (sut, getCaptured) = BuildSutWithCapture();

        var query = new SaleHistoryQuery(
            StaffUserID: 7,
            UserLevel:   "Admin",
            StartDate:   null,
            EndDate:     null,
            Page:        1,
            PageSize:    20);

        await sut.GetSaleHistoryAsync(query);

        var captured = getCaptured();
        Assert.NotNull(captured);
        Assert.Null(captured!.StaffUserID);
    }
}
