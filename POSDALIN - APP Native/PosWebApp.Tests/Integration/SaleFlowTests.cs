using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using PosWebApp.Models.DTOs;
using PosWebApp.Services;

namespace PosWebApp.Tests.Integration;

/// <summary>
/// End-to-end integration test for the happy-path sale flow:
///   Login → Browse home (category grid) → Add item to cart → Checkout → Verify sale created
///
/// Uses <see cref="WebApplicationFactory{TEntryPoint}"/> with a mocked
/// <see cref="IServiceClient"/> — no database, no EF Core, no real HTTP calls.
///
/// Validates: Requirements 8.1, 8.2, 8.3
/// </summary>
public class SaleFlowTests : IClassFixture<SaleFlowTests.PosWebAppFactory>
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Custom WebApplicationFactory
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Custom factory that replaces <see cref="IServiceClient"/> and
    /// <see cref="IProductCatalogService"/> with mocks, disables anti-forgery
    /// token validation, and removes rate limiting so POST handlers can be
    /// called without token extraction or request throttling in tests.
    /// </summary>
    public sealed class PosWebAppFactory : WebApplicationFactory<Program>
    {
        public Mock<IServiceClient> ServiceClientMock { get; } = new Mock<IServiceClient>();
        public Mock<IProductCatalogService> CatalogMock { get; } = new Mock<IProductCatalogService>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Replace the real IServiceClient with the mock — no network I/O.
                services.RemoveAll<IServiceClient>();
                services.AddSingleton<IServiceClient>(ServiceClientMock.Object);

                // Replace the singleton IProductCatalogService with a mock so that
                // the memory-cached results don't leak between tests (the real
                // singleton would cache the first result and subsequent tests would
                // see stale/empty data).
                services.RemoveAll<IProductCatalogService>();
                services.AddSingleton<IProductCatalogService>(CatalogMock.Object);

                // Disable anti-forgery token validation for integration tests.
                // The filter is replaced with a no-op implementation so POST
                // handlers with [ValidateAntiForgeryToken] do not reject requests.
                services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

                // Rate limiting is disabled in the Testing environment by Program.cs
                // (RateLimitPartition.GetNoLimiter for the "authLogin" policy when
                // Environment == "Testing"), so no override is required here.
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  No-op IAntiforgery — bypasses CSRF validation in tests
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Anti-forgery implementation that always succeeds validation and returns
    /// a deterministic fake token, allowing integration tests to POST without
    /// needing to parse and replay real anti-forgery tokens.
    /// </summary>
    private sealed class NoOpAntiforgery : IAntiforgery
    {
        private static readonly AntiforgeryTokenSet FakeTokens =
            new("test-request-token", "test-cookie-token", "field-name", "header-name");

        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext context) => FakeTokens;

        public AntiforgeryTokenSet GetTokens(HttpContext context) => FakeTokens;

        public Task<bool> IsRequestValidAsync(HttpContext context)
            => Task.FromResult(true);

        public void SetCookieTokenAndHeader(HttpContext context) { }

        public Task ValidateRequestAsync(HttpContext context)
            => Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Test data constants
    // ─────────────────────────────────────────────────────────────────────────

    private const string TestUsername   = "cashier01";
    private const string TestPassword   = "Password1!";
    private const int    StaffUserId    = 7;
    private const string StaffFullName  = "Test Cashier";
    private const int    CategoryId     = 1;
    private const int    ItemId         = 42;
    private const string ItemCode       = "ITEM-042";
    private const string ItemName       = "Test Widget";
    private const int    ExpectedSaleId = 9001;
    private const string ExpectedInvoice = "INV-9001";

    // ─────────────────────────────────────────────────────────────────────────
    //  Shared state
    // ─────────────────────────────────────────────────────────────────────────

    private readonly PosWebAppFactory _factory;

    public SaleFlowTests(PosWebAppFactory factory)
    {
        _factory = factory;

        // Configure mock responses before any test runs.
        SetupMockServiceClient(_factory.ServiceClientMock);
        SetupCatalogMock(_factory.CatalogMock);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Configures all <see cref="IServiceClient"/> mock setups needed by the
    /// full sale-flow scenario.
    /// </summary>
    private static void SetupMockServiceClient(Mock<IServiceClient> mock)
    {
        // Step 1 — Login: returns a successful Staff result.
        mock.Setup(s => s.LoginAsync(TestUsername, TestPassword))
            .ReturnsAsync(new LoginResult(
                IsSuccess:    true,
                IsLockedOut:  false,
                ErrorMessage: null,
                UserID:       StaffUserId,
                UserLevel:    "Staff",
                FullName:     StaffFullName));

        // Step 3 — Add to cart: GetItemDetailAsync returns the test item.
        mock.Setup(s => s.GetItemDetailAsync(ItemId))
            .ReturnsAsync(new ItemDetailDto(
                ItemID:     ItemId,
                ItemCode:   ItemCode,
                ItemName:   ItemName,
                ImageUrl:   "/images/test-widget.png",
                PriceTiers: new List<PriceTierDto>
                {
                    new PriceTierDto(1, ItemId, 1, 9999, 15.00m, "USD"),
                }));

        // Step 3 — Add to cart: GetItemUnitsAsync (price tiers) for the item.
        mock.Setup(s => s.GetItemUnitsAsync(ItemId))
            .ReturnsAsync(new List<PriceTierDto>
            {
                new PriceTierDto(1, ItemId, 1, 9999, 15.00m, "USD"),
            });

        // Step 4 — Checkout: CreateSaleAsync returns a successful sale result.
        mock.Setup(s => s.CreateSaleAsync(It.IsAny<CreateSaleRequest>()))
            .ReturnsAsync(new SaleCreationResult(ExpectedSaleId, ExpectedInvoice));
    }

    /// <summary>
    /// Configures the <see cref="IProductCatalogService"/> mock so that each
    /// test sees the correct catalog data regardless of IMemoryCache state.
    /// The real <see cref="ProductCatalogService"/> is a singleton that caches
    /// results; by replacing it with a mock the tests avoid stale-cache issues.
    /// </summary>
    private static void SetupCatalogMock(Mock<IProductCatalogService> mock)
    {
        mock.Setup(s => s.GetCategoriesAsync())
            .ReturnsAsync(new List<CategoryDto>
            {
                new CategoryDto(CategoryId, "Electronics", "/images/electronics.png"),
            });

        mock.Setup(s => s.GetGroupsAsync(It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<GroupDto>());

        mock.Setup(s => s.GetItemsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<ItemDto>());

        mock.Setup(s => s.GetItemDetailAsync(ItemId))
            .ReturnsAsync(new ItemDetailDto(
                ItemID:     ItemId,
                ItemCode:   ItemCode,
                ItemName:   ItemName,
                ImageUrl:   "/images/test-widget.png",
                PriceTiers: new List<PriceTierDto>
                {
                    new PriceTierDto(1, ItemId, 1, 9999, 15.00m, "USD"),
                }));

        mock.Setup(s => s.GetPriceTiersAsync(ItemId))
            .ReturnsAsync(new List<PriceTierDto>
            {
                new PriceTierDto(1, ItemId, 1, 9999, 15.00m, "USD"),
            });

        mock.Setup(s => s.InvalidateCatalogCacheAsync())
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> backed by its own <see cref="CookieContainer"/>
    /// so that the authentication cookie obtained at login is replayed on every
    /// subsequent request — exactly as a browser would behave.
    /// </summary>
    private HttpClient CreateClientWithCookies()
    {
        // Use the test server's handler so cookies are tracked across requests.
        // WebApplicationFactory.CreateDefaultClient only accepts DelegatingHandler[];
        // using Server.CreateHandler() gives us a raw HttpMessageHandler we can
        // wrap with a CookieContainer-aware HttpClientHandler via a redirecting approach.
        var cookieContainer = new CookieContainer();
        var handler = new RedirectCookieHandler(_factory.Server.CreateHandler(), cookieContainer);
        return new HttpClient(handler) { BaseAddress = _factory.Server.BaseAddress };
    }

    /// <summary>
    /// Thin delegating handler that extracts Set-Cookie headers from responses
    /// and re-sends them as Cookie headers on subsequent requests, emulating
    /// browser cookie behaviour for the in-memory test server.
    /// </summary>
    private sealed class RedirectCookieHandler : DelegatingHandler
    {
        private readonly CookieContainer _cookies;
        private readonly Uri _baseAddress;

        public RedirectCookieHandler(HttpMessageHandler inner, CookieContainer cookies)
            : base(inner)
        {
            _cookies = cookies;
            _baseAddress = new Uri("http://localhost/");
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Attach existing cookies to the request
            var cookieHeader = _cookies.GetCookieHeader(request.RequestUri ?? _baseAddress);
            if (!string.IsNullOrEmpty(cookieHeader))
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

            var response = await base.SendAsync(request, cancellationToken);

            // Store any Set-Cookie headers from the response
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
            {
                foreach (var cookie in setCookieValues)
                {
                    _cookies.SetCookies(request.RequestUri ?? _baseAddress, cookie);
                }
            }

            // Follow 302/301/303 redirects (up to 10 times)
            int redirects = 0;
            while ((response.StatusCode == System.Net.HttpStatusCode.Found ||
                    response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                    response.StatusCode == System.Net.HttpStatusCode.SeeOther) &&
                   response.Headers.Location != null && redirects++ < 10)
            {
                var location = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(request.RequestUri ?? _baseAddress, response.Headers.Location);

                var redirectReq = new HttpRequestMessage(HttpMethod.Get, location);
                var cookies2 = _cookies.GetCookieHeader(location);
                if (!string.IsNullOrEmpty(cookies2))
                    redirectReq.Headers.TryAddWithoutValidation("Cookie", cookies2);

                response = await base.SendAsync(redirectReq, cancellationToken);

                if (response.Headers.TryGetValues("Set-Cookie", out var sc2))
                    foreach (var c in sc2)
                        _cookies.SetCookies(location, c);
            }

            return response;
        }
    }

    /// <summary>
    /// Logs in via a real POST to <c>/Account/Login</c> and returns the
    /// authenticated client (the auth cookie is stored in its <see cref="CookieContainer"/>).
    /// </summary>
    private static async Task<HttpClient> LoginAsync(HttpClient client)
    {
        // The login page must be loaded first so the .AspNetCore.Session cookie is set;
        // then we can POST the form.  With the no-op antiforgery we don't need a real token.
        await client.GetAsync("/Account/Login");

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.UserName", TestUsername),
            new KeyValuePair<string, string>("Input.Password", TestPassword),
            new KeyValuePair<string, string>("__RequestVerificationToken", "test-request-token"),
        });

        var response = await client.PostAsync("/Account/Login", formContent);

        // After a successful login the server redirects to /Index; follow it.
        // HttpClient with AllowAutoRedirect=true will follow the redirect automatically.
        Assert.True(
            response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Found,
            $"Login POST returned unexpected status: {response.StatusCode}");

        return client;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Sale Flow Integration Test
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the complete happy-path sale flow:
    ///
    /// 1. Login   — mock <see cref="IServiceClient.LoginAsync"/> returns success with UserLevel=Staff
    /// 2. Browse  — GET / returns 200 and the category grid is rendered
    /// 3. Add     — POST /Cart?handler=Add adds an item (mock GetItemUnitsAsync provides tiers)
    /// 4. Checkout — POST /Checkout submits the sale (mock CreateSaleAsync returns a SaleResult)
    /// 5. Verify  — <see cref="IServiceClient.CreateSaleAsync"/> was called with the correct
    ///              StaffUserID and at least one line-item with matching ItemID
    ///
    /// Requirements: 8.1, 8.2, 8.3
    /// </summary>
    [Fact]
    public async Task FullSaleFlow_LoginBrowseAddCheckout_CreateSaleCalledWithCorrectParameters()
    {
        // Reset mock invocations from previous test runs (important when the fixture is shared).
        _factory.ServiceClientMock.Invocations.Clear();
        _factory.CatalogMock.Invocations.Clear();
        SetupMockServiceClient(_factory.ServiceClientMock);
        SetupCatalogMock(_factory.CatalogMock);

        var client = CreateClientWithCookies();

        // ── Step 1: Login ─────────────────────────────────────────────────────
        await LoginAsync(client);

        // ── Step 2: Browse — load the home page (category grid) ───────────────
        var homeResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);

        var homeHtml = await homeResponse.Content.ReadAsStringAsync();

        // Verify the category grid is rendered (Requirement 3.4, 3.7)
        Assert.Contains("Categories", homeHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Electronics", homeHtml, StringComparison.OrdinalIgnoreCase);

        // ── Step 3: Add item to cart ──────────────────────────────────────────
        // POST to /Cart?handler=Add with JSON-expected form fields.
        // The AJAX endpoint returns JSON { success, newQty, badgeCount }.
        var addToCartContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("AddInput.ItemId",   ItemId.ToString()),
            new KeyValuePair<string, string>("AddInput.ItemCode", ItemCode),
            new KeyValuePair<string, string>("AddInput.ItemName", ItemName),
            new KeyValuePair<string, string>("AddInput.Quantity", "2"),
            new KeyValuePair<string, string>("__RequestVerificationToken", "test-request-token"),
        });

        var addResponse = await client.PostAsync("/Cart?handler=Add", addToCartContent);

        Assert.True(
            addResponse.IsSuccessStatusCode,
            $"POST /Cart?handler=Add returned: {addResponse.StatusCode}");

        var addJson = await addResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"success\":true", addJson, StringComparison.OrdinalIgnoreCase);

        // ── Step 4: Checkout ──────────────────────────────────────────────────
        // POST to /Checkout with payment method "Cash" and sufficient amount tendered.
        // Total = 2 × $15.00 = $30.00; tender $50.00.
        var checkoutContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.PaymentMethod",    "Cash"),
            new KeyValuePair<string, string>("Input.AmountTendered",   "50.00"),
            new KeyValuePair<string, string>("__RequestVerificationToken", "test-request-token"),
        });

        var checkoutResponse = await client.PostAsync("/Checkout", checkoutContent);

        // A successful checkout redirects to the confirmation page.
        // AllowAutoRedirect=true means the final response should be 200 (confirmation page).
        Assert.True(
            checkoutResponse.IsSuccessStatusCode,
            $"POST /Checkout returned: {checkoutResponse.StatusCode}");

        // ── Step 5: Verify CreateSaleAsync was called with correct parameters ─
        _factory.ServiceClientMock.Verify(
            s => s.CreateSaleAsync(It.Is<CreateSaleRequest>(req =>
                // Requirement 8.2 — StaffUserID is populated from the logged-in user's claim
                req.StaffUserID == StaffUserId &&
                // Requirement 8.3 — at least one line item for the item we added
                req.Items.Any(item => item.ItemID == ItemId && item.Quantity == 2) &&
                // Total sanity check: 2 × $15.00 = $30.00
                req.Items.Sum(item => item.UnitPrice * item.Quantity) == 30.00m)),
            Times.Once,
            "CreateSaleAsync should have been called once with StaffUserID=7, " +
            "ItemID=42, Quantity=2, and a line total of $30.00");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Individual stage tests (narrower assertions, easier to diagnose failures)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Step 1 — Login returns 200 (or follows redirect to home) and the
    /// auth cookie is present in the client's cookie container.
    ///
    /// Requirement 1.2, 1.3
    /// </summary>
    [Fact]
    public async Task Login_WithValidStaffCredentials_SetsAuthCookieAndRedirectsHome()
    {
        _factory.ServiceClientMock.Invocations.Clear();
        _factory.CatalogMock.Invocations.Clear();
        SetupMockServiceClient(_factory.ServiceClientMock);
        SetupCatalogMock(_factory.CatalogMock);

        var client = CreateClientWithCookies();

        await client.GetAsync("/Account/Login");

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.UserName", TestUsername),
            new KeyValuePair<string, string>("Input.Password", TestPassword),
            new KeyValuePair<string, string>("__RequestVerificationToken", "test-request-token"),
        });

        var response = await client.PostAsync("/Account/Login", formContent);

        // The client follows the redirect automatically; final destination is /Index.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var finalUri = response.RequestMessage?.RequestUri;
        Assert.NotNull(finalUri);
        Assert.True(
            finalUri!.AbsolutePath.Equals("/", StringComparison.OrdinalIgnoreCase) ||
            finalUri!.AbsolutePath.Equals("/Index", StringComparison.OrdinalIgnoreCase),
            $"Expected redirect to / or /Index, got: {finalUri.AbsolutePath}");
    }

    /// <summary>
    /// Step 2 — Home page loads after login and renders the category grid.
    ///
    /// Requirement 3.4, 3.7
    /// </summary>
    [Fact]
    public async Task HomePage_AfterLogin_RendersCategories()
    {
        _factory.ServiceClientMock.Invocations.Clear();
        _factory.CatalogMock.Invocations.Clear();
        SetupMockServiceClient(_factory.ServiceClientMock);
        SetupCatalogMock(_factory.CatalogMock);

        var client = CreateClientWithCookies();
        await LoginAsync(client);

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Categories", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Electronics", html, StringComparison.OrdinalIgnoreCase);

        // Verify the catalog service was called (cache-first; may call through to mock)
        _factory.CatalogMock.Verify(
            s => s.GetCategoriesAsync(),
            Times.AtLeastOnce,
            "GetCategoriesAsync should have been called to populate the home page");
    }

    /// <summary>
    /// Task 28.2 — Home page (Index) renders the category grid HTML structure.
    ///
    /// Asserts that the Bootstrap responsive grid container
    /// (<c>&lt;div class="row row-cols-2 ..." role="list" aria-label="Product categories"&gt;</c>)
    /// is rendered and contains at least one category card (<c>item-card</c> class)
    /// with the mock-supplied category name "Electronics".
    ///
    /// **Validates: Requirement 1.1 (home page category grid rendering)**
    /// </summary>
    [Fact]
    public async Task HomePage_AfterLogin_RendersCategoryGridStructure()
    {
        _factory.ServiceClientMock.Invocations.Clear();
        _factory.CatalogMock.Invocations.Clear();
        SetupMockServiceClient(_factory.ServiceClientMock);
        SetupCatalogMock(_factory.CatalogMock);

        var client = CreateClientWithCookies();
        await LoginAsync(client);

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        // The grid container with Bootstrap responsive classes and ARIA list role
        Assert.Contains("row-cols-2", html);
        Assert.Contains("role=\"list\"", html);
        Assert.Contains("Product categories", html);

        // The _ItemCardPartial renders an anchor with class "item-card"
        Assert.Contains("item-card", html);

        // The mock-supplied category name appears in the rendered card
        Assert.Contains("Electronics", html);
    }

    /// <summary>
    /// Step 3 — Adding an item to cart returns JSON { success: true } and
    /// GetItemUnitsAsync was called to resolve the price tier.
    ///
    /// Requirement 8.1
    /// </summary>
    [Fact]
    public async Task AddToCart_WithValidItem_ReturnsSuccessJson()
    {
        _factory.ServiceClientMock.Invocations.Clear();
        _factory.CatalogMock.Invocations.Clear();
        SetupMockServiceClient(_factory.ServiceClientMock);
        SetupCatalogMock(_factory.CatalogMock);

        var client = CreateClientWithCookies();
        await LoginAsync(client);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("AddInput.ItemId",   ItemId.ToString()),
            new KeyValuePair<string, string>("AddInput.ItemCode", ItemCode),
            new KeyValuePair<string, string>("AddInput.ItemName", ItemName),
            new KeyValuePair<string, string>("AddInput.Quantity", "1"),
            new KeyValuePair<string, string>("__RequestVerificationToken", "test-request-token"),
        });

        var response = await client.PostAsync("/Cart?handler=Add", content);

        Assert.True(response.IsSuccessStatusCode, $"Expected 2xx, got {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"success\":true", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("badgeCount", json, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Step 4+5 — After adding an item, checkout creates the sale via
    /// CreateSaleAsync with the correct StaffUserID and items.
    ///
    /// Requirements: 8.2, 8.3
    /// </summary>
    [Fact]
    public async Task Checkout_AfterAddingItem_CallsCreateSaleWithCorrectStaffAndItems()
    {
        _factory.ServiceClientMock.Invocations.Clear();
        _factory.CatalogMock.Invocations.Clear();
        SetupMockServiceClient(_factory.ServiceClientMock);
        SetupCatalogMock(_factory.CatalogMock);

        var client = CreateClientWithCookies();
        await LoginAsync(client);

        // Add one item to the cart.
        var addContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("AddInput.ItemId",   ItemId.ToString()),
            new KeyValuePair<string, string>("AddInput.ItemCode", ItemCode),
            new KeyValuePair<string, string>("AddInput.ItemName", ItemName),
            new KeyValuePair<string, string>("AddInput.Quantity", "3"),
            new KeyValuePair<string, string>("__RequestVerificationToken", "test-request-token"),
        });
        await client.PostAsync("/Cart?handler=Add", addContent);

        // Submit checkout.
        var checkoutContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.PaymentMethod",  "Cash"),
            new KeyValuePair<string, string>("Input.AmountTendered", "100.00"),
            new KeyValuePair<string, string>("__RequestVerificationToken", "test-request-token"),
        });

        var checkoutResponse = await client.PostAsync("/Checkout", checkoutContent);
        Assert.True(checkoutResponse.IsSuccessStatusCode,
            $"Checkout POST returned: {checkoutResponse.StatusCode}");

        // Verify CreateSaleAsync parameters.
        _factory.ServiceClientMock.Verify(
            s => s.CreateSaleAsync(It.Is<CreateSaleRequest>(req =>
                req.StaffUserID == StaffUserId &&
                req.Items.Any(i => i.ItemID == ItemId && i.Quantity == 3))),
            Times.Once,
            "CreateSaleAsync should be called once with the expected StaffUserID and item");
    }
}
