using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using PosWebApp.Models.DTOs;
using PosWebApp.Services;

namespace PosWebApp.Tests.Unit.Properties;

/// <summary>
/// Property-based tests for <see cref="ProductCatalogService.SearchPure"/>.
///
/// **Validates: Requirements 10.2, 10.3**
/// </summary>
public class SearchPropertyTests
{
    // ── Arbitrary / generator ──────────────────────────────────────────────────

    /// <summary>
    /// Generates a pair of (search query of 2+ characters, list of ItemDto objects).
    /// The item list may contain items that match and items that do not match the query,
    /// ensuring the filter is exercised against a realistic mixed collection.
    /// </summary>
    public static Arbitrary<(string Query, IReadOnlyList<ItemDto> Items)> QueryAndItemsArb()
    {
        // Build an arbitrary ItemDto from random strings.
        // ItemCode and ItemName are short ASCII words to keep tests readable.
        var itemGen = Gen.Elements("PROD", "ITEM", "GOOD", "CODE", "ALPHA", "BETA", "GAMMA")
            .SelectMany(code =>
                Gen.Elements("Widget", "Gadget", "Device", "Tool", "Product", "Article", "Thing")
                    .SelectMany(name =>
                        Gen.Choose(1, 9999).Select(id =>
                            new ItemDto(id, code + id, name + " " + id, "", 1.00m, 1, 1))));

        // List of 0..20 items so we always exercise both empty and populated cases.
        var listGen = itemGen.ListOf(20).Select(l => (IReadOnlyList<ItemDto>)l);

        // Query: 2 to 8 characters drawn from letters/digits that appear naturally
        // in item names/codes so some items will match and some won't.
        var queryGen = Gen.Elements(
            "Wi",   // matches "Widget"
            "et",   // matches "Widget", "Gadget"
            "OD",   // matches "PROD", "Product"
            "AL",   // matches "ALPHA"
            "99",   // matches items with id ending in 99
            "item", // no match in upper-cased codes but matches "item" if any
            "GA",   // matches "GAMMA", "Gadget"
            "De",   // matches "Device"
            "To",   // matches "Tool"
            "Ar"    // matches "Article"
        );

        var gen = queryGen.SelectMany(q => listGen.Select(items => (q, items)));
        return gen.ToArbitrary();
    }

    // ── Properties ─────────────────────────────────────────────────────────────

    /// <summary>
    /// **Property 8: Search Result Relevance**
    /// For any query of 2+ characters and any item list, every item returned by
    /// <see cref="ProductCatalogService.SearchPure"/> has <c>ItemName</c> or
    /// <c>ItemCode</c> containing the query string (case-insensitive substring match).
    ///
    /// **Validates: Requirements 10.2, 10.3**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(SearchPropertyTests) }, MaxTest = 1000)]
    public Property Property8_AllReturnedItemsContainQueryInNameOrCode(
        (string Query, IReadOnlyList<ItemDto> Items) input)
    {
        var (query, items) = input;

        // Guard: property only applies to queries of 2+ characters.
        if (query.Length < 2)
            return true.ToProperty();

        var results = ProductCatalogService.SearchPure(items, query);

        // Every result item must contain the query in ItemName or ItemCode (case-insensitive).
        foreach (var item in results)
        {
            bool nameMatches = item.ItemName.Contains(query, StringComparison.OrdinalIgnoreCase);
            bool codeMatches = item.ItemCode.Contains(query, StringComparison.OrdinalIgnoreCase);

            if (!nameMatches && !codeMatches)
            {
                return false.Label(
                    $"Item (ID={item.ItemID}, Code='{item.ItemCode}', Name='{item.ItemName}') " +
                    $"was returned for query='{query}' but matches neither ItemName nor ItemCode.");
            }
        }

        return true.ToProperty().Label(
            $"All {results.Count} result(s) contain query='{query}' in ItemName or ItemCode.");
    }

    /// <summary>
    /// **Property 8: Completeness — no matching item is excluded**
    /// If an item's <c>ItemName</c> or <c>ItemCode</c> contains the query, it MUST
    /// appear in the result set.
    ///
    /// **Validates: Requirements 10.2, 10.3**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(SearchPropertyTests) }, MaxTest = 1000)]
    public Property Property8_NoMatchingItemIsExcluded(
        (string Query, IReadOnlyList<ItemDto> Items) input)
    {
        var (query, items) = input;

        if (query.Length < 2)
            return true.ToProperty();

        var results = ProductCatalogService.SearchPure(items, query);
        var resultIds = new HashSet<int>(results.Select(r => r.ItemID));

        foreach (var item in items)
        {
            bool shouldMatch =
                item.ItemName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.ItemCode.Contains(query, StringComparison.OrdinalIgnoreCase);

            if (shouldMatch && !resultIds.Contains(item.ItemID))
            {
                return false.Label(
                    $"Item (ID={item.ItemID}, Code='{item.ItemCode}', Name='{item.ItemName}') " +
                    $"matches query='{query}' but was NOT included in the results.");
            }
        }

        return true.ToProperty().Label(
            $"All items matching query='{query}' are present in the {results.Count} result(s).");
    }

    /// <summary>
    /// **Property 8: Blank/whitespace query returns empty**
    /// When the query is blank or whitespace, <c>SearchPure</c> returns an empty
    /// collection regardless of the item list contents.
    ///
    /// **Validates: Requirements 10.2, 10.3**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(SearchPropertyTests) }, MaxTest = 500)]
    public Property Property8_BlankQueryReturnsEmpty(
        (string Query, IReadOnlyList<ItemDto> Items) input)
    {
        // SearchPure returns empty for null/whitespace queries.
        var results = ProductCatalogService.SearchPure(input.Items, "   ");

        return (results.Count == 0).Label(
            $"Whitespace-only query should return 0 results, got {results.Count}.");
    }

    // ── Example-based edge cases ──────────────────────────────────────────────

    [Fact]
    public void EmptyItemList_ReturnsEmptyResult()
    {
        var results = ProductCatalogService.SearchPure(
            Array.Empty<ItemDto>(), "Widget");
        Assert.Empty(results);
    }

    [Fact]
    public void ExactItemCode_Match_ReturnsItem()
    {
        var items = new List<ItemDto>
        {
            new ItemDto(1, "ABC001", "Widget Pro", "", 5.00m, 1, 1),
            new ItemDto(2, "XYZ999", "Gadget Plus", "", 3.00m, 1, 1),
        };
        var results = ProductCatalogService.SearchPure(items, "ABC001");
        Assert.Single(results);
        Assert.Equal(1, results[0].ItemID);
    }

    [Fact]
    public void CaseInsensitive_ItemName_Match()
    {
        var items = new List<ItemDto>
        {
            new ItemDto(1, "ITEM01", "Wireless Mouse", "", 10.00m, 1, 1),
            new ItemDto(2, "ITEM02", "USB Keyboard", "", 8.00m, 1, 1),
        };

        // Uppercase query should match lower-case name
        var results = ProductCatalogService.SearchPure(items, "WIRELESS");
        Assert.Single(results);
        Assert.Equal(1, results[0].ItemID);
    }

    [Fact]
    public void PartialQuery_MatchesBothNameAndCode()
    {
        var items = new List<ItemDto>
        {
            new ItemDto(1, "GA001", "Gadget Pro", "", 5.00m, 1, 1),  // code matches "GA"
            new ItemDto(2, "XY002", "Galaxy Tab", "", 9.00m, 1, 1),  // name matches "GA" (Galaxy)
            new ItemDto(3, "ZZ003", "Monitor 4K", "", 20.00m, 1, 1), // neither
        };
        var results = ProductCatalogService.SearchPure(items, "ga");
        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, r => r.ItemID == 3);
    }

    [Fact]
    public void SingleCharQuery_FiltersNormally()
    {
        // SearchPure has no minimum-length guard — the 2-char minimum is enforced
        // at the SearchItemsAsync service layer. A single-char query filters by
        // substring match just like any other query.
        var items = new List<ItemDto>
        {
            new ItemDto(1, "A001", "Widget", "", 2.00m, 1, 1),  // code "A001" contains "A"
            new ItemDto(2, "B002", "Box",    "", 1.50m, 1, 1),  // neither code nor name contains "A"
        };
        // "A" (case-insensitive) matches item 1 (code "A001"), not item 2
        var results = ProductCatalogService.SearchPure(items, "A");
        Assert.Single(results);
        Assert.Equal(1, results[0].ItemID);
    }

    [Fact]
    public void BlankQuery_ReturnsEmpty()
    {
        var items = new List<ItemDto>
        {
            new ItemDto(1, "A001", "Apple Juice", "", 2.00m, 1, 1),
        };
        var results = ProductCatalogService.SearchPure(items, "  ");
        Assert.Empty(results);
    }
}
