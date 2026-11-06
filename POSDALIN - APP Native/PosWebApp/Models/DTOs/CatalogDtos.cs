namespace PosWebApp.Models.DTOs;

/// <summary>Product category returned by getcategory endpoint.</summary>
public record CategoryDto(int CategoryID, string CategoryName, string ImageUrl);

/// <summary>Product group returned by getgroup endpoint.</summary>
public record GroupDto(int GroupID, string GroupName, string ImageUrl, int CategoryID);

/// <summary>Product item summary returned by getSaleItem / searchItems endpoints.</summary>
public record ItemDto(
    int ItemID,
    string ItemCode,
    string ItemName,
    string ImageUrl,
    double Price,
    int CategoryID,
    int GroupID);

/// <summary>Full item detail including price tiers, returned by getitemdetail endpoint.</summary>
public record ItemDetailDto(
    int ItemID,
    string ItemCode,
    string ItemName,
    string ImageUrl,
    IReadOnlyList<PriceTierDto> PriceTiers);

/// <summary>Single price tier entry returned by getitemunit endpoint.</summary>
public record PriceTierDto(
    int ID,
    int ItemID,
    int FromQty,
    int ToQty,
	double Price,
    string Currency);

/// <summary>
/// Full product row returned by Sp_getSaleItem_Client2 stored procedure.
/// Contains item, group, and category info in a single flat result set.
/// This is the single source of product data cached on the server.
/// </summary>
public record ProductCatalogDto(
    int ItemID,
    string ItemCode,
    string ItemName,
    string ImageUrl,
    double Price,
    string Currency,
    int GroupID,
    string GroupName,
    int CategoryID,
    string CategoryName,
    int SaleID);