using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PosWebApp.Infrastructure.Exceptions;
using PosWebApp.Models.DTOs;

namespace PosWebApp.Services;

/// <summary>
/// ADO.NET implementation of <see cref="IServiceClient"/> that calls
/// SQL Server stored procedures directly. Replaces the former AsmxServiceClient
/// which used HTTP/SOAP to call ASMX endpoints.
///
/// All methods use <see cref="SqlCommand"/> with <see cref="CommandType.StoredProcedure"/>,
/// pass parameters via <see cref="SqlParameter"/> objects (SQL injection prevention),
/// and map <see cref="SqlDataReader"/> results to C# DTOs.
///
/// All <see cref="SqlException"/>s are wrapped in <see cref="ServiceException"/>
/// so callers never see raw SQL exceptions.
///
/// **Validates: Requirements 24.1-24.16, 19.1, 19.5**
/// </summary>
public class SqlServiceClient : IServiceClient
{
    private readonly string _connectionString;
    private readonly int _commandTimeout;
    private readonly string _imageBaseUrl;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SqlServiceClient> _logger;

    public SqlServiceClient(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<SqlServiceClient> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        _commandTimeout = configuration.GetValue<int>("Database:CommandTimeoutSeconds", 30);
        _imageBaseUrl = configuration.GetValue<string>("Database:ImageBaseUrl") ?? string.Empty;
        _httpClient = httpClient;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Authentication
    // ════════════════════════════════════════════════════════════════════════

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        _logger.LogInformation("LoginAsync - username: {Username}", username);

        try
        {

            DataSet ds = new DataSet();
			await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = CreateCommand(conn, "Sp_LoginSB", _commandTimeout);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);
   
			using (SqlDataAdapter da = new SqlDataAdapter(cmd))
			{
				da.Fill(ds);
			}


			return MapLoginResult(ds.Tables[0]);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error in LoginAsync");
            throw new AuthenticationException("Login failed. Please try again.", ex);
        }
        catch (Exception ex) when (ex is not ServiceException)
        {
            _logger.LogError(ex, "Error in LoginAsync");
            throw new AuthenticationException("Login failed. Please try again.", ex);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Product catalog
    // ════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync()
    {
        _logger.LogInformation("GetCategoriesAsync - via Sp_getSaleItem_Client2");

        try
        {
            var products = await GetAllProductsAsync();
            return products
                .Select(p => new { p.CategoryID, p.CategoryName })
                .Distinct()
                .Select(c => new CategoryDto(c.CategoryID, c.CategoryName, string.Empty))
                .ToList();
        }
        catch (SqlException ex) { throw WrapSql(ex, "Failed to retrieve categories"); }
        catch (Exception ex) when (ex is not ServiceException) { throw Wrap(ex, "Failed to retrieve categories"); }
    }

    public async Task<IReadOnlyList<GroupDto>> GetGroupsAsync(int categoryId)
    {
        _logger.LogInformation("GetGroupsAsync - categoryId: {CategoryId}", categoryId);

        try
        {
            var products = await GetAllProductsAsync();
            return products
                .Where(p => p.CategoryID == categoryId)
                .Select(p => new { p.GroupID, p.GroupName, p.CategoryID })
                .Distinct()
                .Select(g => new GroupDto(g.GroupID, g.GroupName, string.Empty, g.CategoryID))
                .ToList();
        }
        catch (SqlException ex) { throw WrapSql(ex, "Failed to retrieve groups"); }
        catch (Exception ex) when (ex is not ServiceException) { throw Wrap(ex, "Failed to retrieve groups"); }
    }

    public async Task<IReadOnlyList<ItemDto>> GetSaleItemsAsync(int categoryId, int groupId)
    {
        _logger.LogInformation("GetSaleItemsAsync - categoryId: {CategoryId}, groupId: {GroupId}", categoryId, groupId);

        try
        {
            var products = await GetAllProductsAsync();
            return products
                .Where(p => p.CategoryID == categoryId && (groupId == 0 || p.GroupID == groupId))
                .Select(p => new ItemDto(p.ItemID, p.ItemCode, p.ItemName, p.ImageUrl, p.Price, p.CategoryID, p.GroupID))
                .ToList();
        }
        catch (SqlException ex) { throw WrapSql(ex, "Failed to retrieve sale items"); }
        catch (Exception ex) when (ex is not ServiceException) { throw Wrap(ex, "Failed to retrieve sale items"); }
    }

    public async Task<ItemDetailDto> GetItemDetailAsync(int itemId)
    {
        _logger.LogInformation("GetItemDetailAsync - itemId: {ItemId}", itemId);

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // First result set: item detail
            using var cmd = CreateCommand(conn, "SP_GetItemDetail", _commandTimeout);
            cmd.Parameters.AddWithValue("@ItemID", itemId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new ServiceException($"Item {itemId} not found.");

            var detail = new ItemDetailDto(
                ItemID: reader.GetInt32("ItemID"),
                ItemCode: reader.GetStringOrNull("ItemCode"),
                ItemName: reader.GetStringOrNull("ItemName"),
                ImageUrl: reader.GetStringOrNull("ImageUrl"),
                PriceTiers: new List<PriceTierDto>());

            // Second result set: price tiers (if NextResult returns true)
            var tiers = new List<PriceTierDto>();
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                    tiers.Add(MapPriceTier(reader));
            }
            else
            {
                // Price tiers might be in a separate call
                tiers.AddRange(await GetItemUnitsInternalAsync(conn, itemId));
            }

            return detail with { PriceTiers = tiers };
        }
        catch (SqlException ex) { throw WrapSql(ex, "Failed to retrieve item detail"); }
        catch (Exception ex) when (ex is not ServiceException) { throw Wrap(ex, "Failed to retrieve item detail"); }
    }

    public async Task<IReadOnlyList<PriceTierDto>> GetItemUnitsAsync(int itemId)
    {
        _logger.LogInformation("GetItemUnitsAsync - itemId: {ItemId}", itemId);

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            return await GetItemUnitsInternalAsync(conn, itemId);
        }
        catch (SqlException ex) { throw WrapSql(ex, "Failed to retrieve item units"); }
        catch (Exception ex) when (ex is not ServiceException) { throw Wrap(ex, "Failed to retrieve item units"); }
    }

    public async Task<IReadOnlyList<ItemDto>> SearchItemsAsync(string keyword, int page, int pageSize)
    {
        _logger.LogInformation("SearchItemsAsync - keyword: {Keyword}, page: {Page}, pageSize: {PageSize}", keyword, page, pageSize);

        try
        {
            // Filter from cached catalog instead of separate database call
            var products = await GetAllProductsAsync();
            
            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? products
                : products.Where(p => 
                    (p.ItemName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.ItemCode?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));

            var paged = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ItemDto(p.ItemID, p.ItemCode, p.ItemName, p.ImageUrl, p.Price, p.CategoryID, p.GroupID))
                .ToList();

            return paged;
        }
        catch (SqlException ex) { throw WrapSql(ex, "Failed to search items"); }
        catch (Exception ex) when (ex is not ServiceException) { throw Wrap(ex, "Failed to search items"); }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Sales
    // ════════════════════════════════════════════════════════════════════════

    public async Task<SaleCreationResult> CreateSaleAsync(CreateSaleRequest request)
    {
        _logger.LogInformation("CreateSaleAsync - staffUserId: {StaffUserID}, items: {ItemCount}", request.StaffUserID, request.Items.Count);

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = CreateCommand(conn, "CreateSale", _commandTimeout);
            cmd.Parameters.AddWithValue("@StaffUserID", request.StaffUserID);
            cmd.Parameters.AddWithValue("@CustomerID", (object?)request.CustomerID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PaymentMethod", request.PaymentMethod);

            // Pass items as JSON for the stored procedure to parse via OPENJSON
            var itemsJson = JsonSerializer.Serialize(request.Items.Select(i => new
            {
                ItemID = i.ItemID,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Discount = i.Discount
            }));
            cmd.Parameters.AddWithValue("@Items", itemsJson);

            // Output parameters for SaleID and InvoiceNo
            var saleIdParam = new SqlParameter("@SaleID", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            var invoiceParam = new SqlParameter("@InvoiceNo", System.Data.SqlDbType.NVarChar, 50) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(saleIdParam);
            cmd.Parameters.Add(invoiceParam);

            await cmd.ExecuteNonQueryAsync();

            var saleId = saleIdParam.Value is int sid ? sid : Convert.ToInt32(saleIdParam.Value);
            var invoiceNo = invoiceParam.Value?.ToString() ?? "";
            return new SaleCreationResult(saleId, invoiceNo);
        }
        catch (SqlException ex) { throw new SaleProcessingException("Failed to create sale. Please try again.", ex); }
        catch (Exception ex) when (ex is not ServiceException) { throw new SaleProcessingException("Failed to create sale. Please try again.", ex); }
    }

    public async Task<PagedResult<SaleListItemDto>> GetSaleHistoryAsync(SaleHistoryQuery query)
    {
        _logger.LogInformation("GetSaleHistoryAsync - staffUserId: {StaffUserID}, userLevel: {UserLevel}, page: {Page}", query.StaffUserID, query.UserLevel, query.Page);

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = CreateCommand(conn, "GetSaleHistory", _commandTimeout);
            cmd.Parameters.AddWithValue("@StaffUserID", (object?)query.StaffUserID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserLevel", query.UserLevel);
            cmd.Parameters.AddWithValue("@StartDate", (object?)query.StartDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EndDate", (object?)query.EndDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Page", query.Page);
            cmd.Parameters.AddWithValue("@PageSize", query.PageSize);

            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<SaleListItemDto>();
            var totalCount = 0;

            while (await reader.ReadAsync())
                list.Add(MapSaleListItem(reader));

            // Check for a second result set with total count
            if (await reader.NextResultAsync() && await reader.ReadAsync())
                totalCount = reader.GetInt32(0);

            if (totalCount == 0) totalCount = list.Count;
            return new PagedResult<SaleListItemDto>(list, totalCount, query.Page, query.PageSize);
        }
        catch (SqlException ex) { throw WrapSql(ex, "Failed to retrieve sale history"); }
        catch (Exception ex) when (ex is not ServiceException) { throw Wrap(ex, "Failed to retrieve sale history"); }
    }

    public async Task<SaleDetailDto> GetSaleDetailAsync(int saleId)
    {
        _logger.LogInformation("GetSaleDetailAsync - saleId: {SaleId}", saleId);

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = CreateCommand(conn, "GetSaleDetail", _commandTimeout);
            cmd.Parameters.AddWithValue("@SaleID", saleId);

            await using var reader = await cmd.ExecuteReaderAsync();

            // First result set: sale header
            if (!await reader.ReadAsync())
                throw new ServiceException($"Sale {saleId} not found.");

            var sale = new SaleDetailDto(
                SaleID: reader.GetInt32("SaleID"),
                InvoiceNo: reader.GetStringOrNull("InvoiceNo"),
                CreatedDate: reader.GetDateTime("CreatedDate"),
                StaffUserName: reader.GetStringOrNull("StaffUserName"),
                CustomerName: reader.GetStringOrNull("CustomerName"),
                Total: reader.GetDouble("Total"),
                PaymentMethod: reader.GetStringOrNull("PaymentMethod") ?? "Cash",
                Items: new List<SaleItemDto>(),
                Voided: reader.GetBoolOrNull("Voided") ?? false,
                VoidedBy: reader.GetStringOrNull("VoidedBy"),
                VoidedDate: reader.GetNullableDateTime("VoidedDate"),
                VoidReason: reader.GetStringOrNull("VoidReason"));

            // Second result set: sale items
            var items = new List<SaleItemDto>();
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                    items.Add(MapSaleItem(reader));
            }

            return sale with { Items = items };
        }
        catch (SqlException ex) { throw WrapSql(ex, "Failed to retrieve sale detail"); }
        catch (Exception ex) when (ex is not ServiceException) { throw Wrap(ex, "Failed to retrieve sale detail"); }
    }

    public async Task<bool> VoidSaleAsync(int saleId, string reason, int staffUserId)
    {
        _logger.LogInformation("VoidSaleAsync - saleId: {SaleId}, staffUserId: {StaffUserId}", saleId, staffUserId);

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = CreateCommand(conn, "VoidSale", _commandTimeout);
            cmd.Parameters.AddWithValue("@SaleID", saleId);
            cmd.Parameters.AddWithValue("@reason", reason);
            cmd.Parameters.AddWithValue("@StaffUserID", staffUserId);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (SqlException ex) { throw new SaleProcessingException("Failed to void sale. Please try again.", ex); }
        catch (Exception ex) when (ex is not ServiceException) { throw new SaleProcessingException("Failed to void sale. Please try again.", ex); }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Customers
    // ════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string query)
    {
        _logger.LogInformation("SearchCustomersAsync - query: {Query}", query);

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = CreateCommand(conn, "SearchCustomers", _commandTimeout);
            cmd.Parameters.AddWithValue("@query", query);

            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<CustomerDto>();
            while (await reader.ReadAsync())
                list.Add(new CustomerDto(
                    CustomerID: reader.GetInt32("CustomerID"),
                    CustomerName: reader.GetStringOrNull("CustomerName"),
                    Phone: reader.GetStringOrNull("Phone")));
            return list;
        }
        catch (SqlException ex) { throw WrapSql(ex, "Failed to search customers"); }
        catch (Exception ex) when (ex is not ServiceException) { throw Wrap(ex, "Failed to search customers"); }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Full catalog (single source - Sp_getSaleItem_Client2)
    // ════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductCatalogDto>> GetAllProductsAsync()
    {
        _logger.LogInformation("GetAllProductsAsync - fetching all products via Sp_getSaleItem_Client2");

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = CreateCommand(conn, "Sp_getSaleItem_Client2", _commandTimeout);
            cmd.Parameters.AddWithValue("@GroupID", 0);
            cmd.Parameters.AddWithValue("@CategoryID", 0);

            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<ProductCatalogDto>();
            while (await reader.ReadAsync())
                list.Add(MapProductCatalog(reader));
            _logger.LogInformation("GetAllProductsAsync - returned {Count} products", list.Count);
            return list;
        }
        catch (SqlException ex) { throw WrapSql(ex, "Failed to retrieve all products"); }
        catch (Exception ex) when (ex is not ServiceException) { throw Wrap(ex, "Failed to retrieve all products"); }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Images
    // ════════════════════════════════════════════════════════════════════════

    public async Task<Stream> GetImageStreamAsync(string imagePath)
    {
        _logger.LogInformation("GetImageStreamAsync - path: {ImagePath}", imagePath);

        if (string.IsNullOrWhiteSpace(_imageBaseUrl))
        {
            // No image server configured - return empty stream (placeholder will be shown)
            _logger.LogWarning("No ImageBaseUrl configured - returning empty stream for {ImagePath}", imagePath);
            return new MemoryStream();
        }

        try
        {
            var requestUri = string.IsNullOrWhiteSpace(imagePath) ? "hdImage_all.ashx" : imagePath;
            if (!Uri.IsWellFormedUriString(requestUri, UriKind.Absolute))
                requestUri = _imageBaseUrl.TrimEnd('/') + "/" + requestUri.TrimStart('/');

            var response = await _httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Image request failed: {StatusCode}", response.StatusCode);
                return new MemoryStream();
            }
            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception ex) when (ex is not ServiceException)
        {
            _logger.LogError(ex, "Failed to retrieve image: {ImagePath}", imagePath);
            return new MemoryStream();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Private helpers
    // ════════════════════════════════════════════════════════════════════════

    private static SqlCommand CreateCommand(SqlConnection conn, string spName, int timeout)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.CommandText = spName;
        cmd.CommandTimeout = timeout;
        return cmd;
    }

    private async Task<List<PriceTierDto>> GetItemUnitsInternalAsync(SqlConnection conn, int itemId)
    {
        using var cmd = CreateCommand(conn, "Sp_getItemUnit", _commandTimeout);
        cmd.Parameters.AddWithValue("@ItemID", itemId);

        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<PriceTierDto>();
        while (await reader.ReadAsync())
            list.Add(MapPriceTier(reader));
        return list;
    }

    private static LoginResult MapLoginResult(DataTable dt)
    {
        var status = dt.Rows[0]["status"] as string;

        var isSuccess = false;
		var errorMessage = "" as string;
		if (status == "Active")
        {
            isSuccess = true;
		}
        else
        {
            if (status == "Invalid")
            {
                errorMessage = "Invalid username or password.";
            }else if (status == "Locked")
            {
                errorMessage = "Your account is locked.";
            }
            else
            {
                errorMessage = "Failed to login.";
            }
        }
        var isLockedOut = false;        
        var userId = dt.Rows[0]["UserID"] as int? ?? 0;
        var userLevel = dt.Rows[0]["UserLevel"] as string ?? "";
        var fullName = dt.Rows[0]["UserName"] as string ?? "";

        // If UserID > 0, treat as success
        if (userId > 0) isSuccess = true;
        if (isLockedOut) isSuccess = false;

        return new LoginResult(isSuccess, isLockedOut, errorMessage, userId, userLevel, fullName);
    }

    private static CategoryDto MapCategory(SqlDataReader r) => new(
        CategoryID: r.GetInt32("CategoryID"),
        CategoryName: r.GetStringOrNull("CategoryName") ?? "",
        ImageUrl: r.GetStringOrNull("ImageUrl") ?? "");

    private static GroupDto MapGroup(SqlDataReader r) => new(
        GroupID: r.GetInt32("GroupID"),
        GroupName: r.GetStringOrNull("GroupName") ?? "",
        ImageUrl: r.GetStringOrNull("ImageUrl") ?? "",
        CategoryID: r.GetInt32("CategoryID"));

    private static ItemDto MapItem(SqlDataReader r) => new(
        ItemID: r.GetInt32("ItemID"),
        ItemCode: r.GetStringOrNull("ItemCode") ?? "",
        ItemName: r.GetStringOrNull("ItemName") ?? "",
        ImageUrl: r.GetStringOrNull("ImageUrl") ?? "",
        Price: r.GetDoubleOrNull("Price") ?? 0,
        CategoryID: r.GetInt32OrNull("CategoryID") ?? 0,
        GroupID: r.GetInt32OrNull("GroupID") ?? 0);

    private static PriceTierDto MapPriceTier(SqlDataReader r) => new(
        ID: r.GetInt32OrNull("ID") ?? 0,
        ItemID: r.GetInt32("ItemID"),
        FromQty: r.GetInt32OrNull("FromQty") ?? 0,
        ToQty: r.GetInt32OrNull("ToQty") ?? 0,
        Price: r.GetDoubleOrNull("Price") ?? 0,
        Currency: r.GetStringOrNull("Currency") ?? "USD");

    private static SaleListItemDto MapSaleListItem(SqlDataReader r) => new(
        SaleID: r.GetInt32("SaleID"),
        InvoiceNo: r.GetStringOrNull("InvoiceNo") ?? "",
        CreatedDate: r.GetDateTime("CreatedDate"),
        StaffUserName: r.GetStringOrNull("StaffUserName") ?? "",
        CustomerName: r.GetStringOrNull("CustomerName"),
        Total: r.GetDoubleOrNull("Total") ?? 0,
        Voided: r.GetBoolOrNull("Voided") ?? false);

    private static SaleItemDto MapSaleItem(SqlDataReader r) => new(
        SaleItemID: r.GetInt32OrNull("SaleItemID") ?? 0,
        ItemID: r.GetInt32("ItemID"),
        ItemCode: r.GetStringOrNull("ItemCode") ?? "",
        ItemName: r.GetStringOrNull("ItemName") ?? "",
        Quantity: r.GetInt32("Quantity"),
        UnitPrice: r.GetDoubleOrNull("UnitPrice") ?? 0,
        Discount: r.GetDoubleOrNull("Discount") ?? 0,
        LineTotal: r.GetDoubleOrNull("LineTotal") ?? 0);

    private static ProductCatalogDto MapProductCatalog(SqlDataReader r) => new(
        ItemID: r.GetInt32("ItemID"),
        ItemCode: r.GetStringOrNull("ItemCode") ?? "",
        ItemName: r.GetStringOrNull("ItemName") ?? "",
        ImageUrl: r.GetStringOrNull("ImageUrl") ?? "",
        Price: r.GetDoubleOrNull("Price") ?? 0,
        Currency: r.GetStringOrNull("Currency") ?? "USD",
        GroupID: r.GetInt32OrNull("GroupID") ?? 0,
        GroupName: r.GetStringOrNull("GroupName") ?? "",
        CategoryID: r.GetInt32OrNull("CategoryID") ?? 0,
        CategoryName: r.GetStringOrNull("CategoryName") ?? "",
        SaleID: r.GetInt32OrNull("ID") ?? r.GetInt32OrNull("SaleID") ?? 0);

    private static ServiceException WrapSql(SqlException ex, string msg)
        => new ServiceException($"{msg}. Please try again.", ex);

    private static ServiceException Wrap(Exception ex, string msg)
        => ex is ServiceException se ? se : new ServiceException($"{msg}. Please try again.", ex);
}

// ── SqlDataReader extension helpers ──────────────────────────────────────────

internal static class SqlReaderExtensions
{
    public static string? GetStringOrNull(this SqlDataReader r, string name)
        => r.GetOrdinal(name) is int i && i >= 0 && !r.IsDBNull(i) ? r.GetString(i) : null;

    public static int GetInt32(this SqlDataReader r, string name)
        => r.GetInt32(r.GetOrdinal(name));

    public static int? GetInt32OrNull(this SqlDataReader r, string name)
        => r.GetOrdinal(name) is int i && i >= 0 && !r.IsDBNull(i) ? r.GetInt32(i) : null;

    public static decimal GetDecimal(this SqlDataReader r, string name)
        => r.GetDecimal(r.GetOrdinal(name));

    public static decimal? GetDecimalOrNull(this SqlDataReader r, string name)
        => r.GetOrdinal(name) is int i && i >= 0 && !r.IsDBNull(i) ? r.GetDecimal(i) : null;

	public static double? GetDoubleOrNull(this SqlDataReader r, string name)
	=> r.GetOrdinal(name) is int i && i >= 0 && !r.IsDBNull(i) ? r.GetDouble(i) : null;
	public static DateTime GetDateTime(this SqlDataReader r, string name)
        => r.GetDateTime(r.GetOrdinal(name));

    public static DateTime? GetNullableDateTime(this SqlDataReader r, string name)
        => r.GetOrdinal(name) is int i && i >= 0 && !r.IsDBNull(i) ? r.GetDateTime(i) : null;

    public static bool? GetBoolOrNull(this SqlDataReader r, string name)
        => r.GetOrdinal(name) is int i && i >= 0 && !r.IsDBNull(i) ? r.GetBoolean(i) : null;
}