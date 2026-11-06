using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PosWebApp.Models.DTOs;
using PosWebApp.Services;

namespace PosWebApp.Tests.Integration;

// ─────────────────────────────────────────────────────────────────────────────
//  Test auth handler — signs in a fake Staff user automatically so that
//  pages protected with [Authorize] render without redirecting to login.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Options carrier for <see cref="TestAuthHandler"/>.
/// </summary>
public class TestAuthHandlerOptions : AuthenticationSchemeOptions { }

/// <summary>
/// A no-op authentication handler that returns a pre-built Staff identity for
/// every request, bypassing the normal cookie / credential flow.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<TestAuthHandlerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "teststaff"),
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Staff"),
            new Claim("UserID", "1"),
            new Claim("UserLevel", "Staff"),
            new Claim("FullName", "Test Staff"),
        };

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Shared factory helpers
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Shared helpers for building <see cref="HttpClient"/> instances with or without
/// the test authentication scheme injected.
/// </summary>
internal static class TestFactoryHelpers
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> that does NOT inject the test auth
    /// handler — used for pages that should be publicly accessible (e.g. Login).
    /// IServiceClient is still mocked so no real backend calls are made.
    /// </summary>
    public static HttpClient CreateAnonymousClient(WebApplicationFactory<Program> factory)
    {
        var serviceClientMock = BuildServiceClientMock();

        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Replace the real HttpClient-backed IServiceClient with a mock
                services.RemoveAll<IServiceClient>();
                services.AddSingleton(serviceClientMock.Object);
            });
        })
        .CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> that is automatically authenticated
    /// as a Staff user via <see cref="TestAuthHandler"/>.
    /// IServiceClient and IProductCatalogService are mocked.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var serviceClientMock = BuildServiceClientMock();
        var catalogMock       = BuildCatalogMock();

        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Replace the real IServiceClient with a mock
                services.RemoveAll<IServiceClient>();
                services.AddSingleton(serviceClientMock.Object);

                // Replace the real IProductCatalogService (singleton) with a mock
                // that returns an empty category list so the Index page renders
                // without real DB/backend calls.
                services.RemoveAll<IProductCatalogService>();
                services.AddSingleton(catalogMock.Object);

                // Inject the test auth handler and set it as the default scheme
                // so [Authorize] pages use it instead of the real cookie handler.
                services
                    .AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });
        })
        .CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static Mock<IServiceClient> BuildServiceClientMock()
    {
        var mock = new Mock<IServiceClient>();

        // Provide default returns for anything that may be called during startup
        mock.Setup(s => s.GetCategoriesAsync())
            .ReturnsAsync(Array.Empty<CategoryDto>());

        return mock;
    }

    private static Mock<IProductCatalogService> BuildCatalogMock()
    {
        var mock = new Mock<IProductCatalogService>();

        mock.Setup(s => s.GetCategoriesAsync())
            .ReturnsAsync(Array.Empty<CategoryDto>());

        mock.Setup(s => s.GetGroupsAsync(It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<GroupDto>());

        mock.Setup(s => s.GetItemsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<ItemDto>());

        return mock;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Task 28.2 — Razor Page rendering tests
//  Validates: Requirements 1.1, 17.10
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Integration tests that spin up the full ASP.NET Core pipeline via
/// <see cref="WebApplicationFactory{TEntryPoint}"/> and assert on the rendered
/// HTML content of key Razor Pages.
///
/// <list type="bullet">
///   <item>Login page — unauthenticated GET, asserts input fields are present</item>
///   <item>Home page  — authenticated GET, asserts category grid container present</item>
///   <item>Cart page  — authenticated GET with empty session, asserts empty-cart message</item>
/// </list>
///
/// **Validates: Requirements 1.1, 17.10**
/// </summary>
public class RazorPageRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RazorPageRenderingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ── Test 1: Login page ────────────────────────────────────────────────────

    /// <summary>
    /// Requirement 1.1 — GET /Account/Login renders a username and a password
    /// input field on the login page.
    /// </summary>
    [Fact]
    public async Task LoginPage_Get_RendersUsernameAndPasswordInputFields()
    {
        // Arrange
        var client = TestFactoryHelpers.CreateAnonymousClient(_factory);

        // Act
        var response = await client.GetAsync("/Account/Login");

        // Assert — page returns 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        // Requirement 1.1: username input field
        Assert.Contains("Input_UserName", html);

        // Requirement 1.1: password input field
        Assert.Contains("Input_Password", html);

        // Both should be <input> elements
        Assert.Contains("<input", html);
    }

    /// <summary>
    /// Login page contains the login form with method="post".
    /// </summary>
    [Fact]
    public async Task LoginPage_Get_ContainsLoginForm()
    {
        // Arrange
        var client = TestFactoryHelpers.CreateAnonymousClient(_factory);

        // Act
        var response = await client.GetAsync("/Account/Login");
        var html     = await response.Content.ReadAsStringAsync();

        // Assert — a POST form is present
        Assert.Contains("method=\"post\"", html);

        // The submit button is present
        Assert.Contains("Log in", html);
    }

    // ── Test 2: Home page ─────────────────────────────────────────────────────

    /// <summary>
    /// Requirement 3.4 — GET / (authenticated) renders the category grid container.
    /// When no categories are available the "No categories available" message is shown.
    /// </summary>
    [Fact]
    public async Task HomePage_AuthenticatedGet_RendersPageWithCategoriesHeader()
    {
        // Arrange
        var client = TestFactoryHelpers.CreateAuthenticatedClient(_factory);

        // Act
        var response = await client.GetAsync("/");

        // Assert — authenticated users reach the home page (200, not 302)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        // The page title / heading "Categories" is always rendered
        Assert.Contains("Categories", html);
    }

    /// <summary>
    /// GET / with an empty category list renders the "No categories available" notice.
    /// </summary>
    [Fact]
    public async Task HomePage_AuthenticatedGet_EmptyCategories_RendersEmptyState()
    {
        // Arrange — catalog mock returns an empty list (default in CreateAuthenticatedClient)
        var client = TestFactoryHelpers.CreateAuthenticatedClient(_factory);

        // Act
        var response = await client.GetAsync("/");
        var html     = await response.Content.ReadAsStringAsync();

        // Assert — empty-state text from Index.cshtml
        Assert.Contains("No categories available", html);
    }

    /// <summary>
    /// GET / without authentication redirects to the login page.
    /// </summary>
    [Fact]
    public async Task HomePage_UnauthenticatedGet_RedirectsToLogin()
    {
        // Arrange — client with no auth handler
        var client = TestFactoryHelpers.CreateAnonymousClient(_factory);

        // Act
        var response = await client.GetAsync("/");

        // Assert — 302 redirect (AllowAutoRedirect = false)
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/Account/Login", location);
    }

    // ── Test 3: Cart page (empty) ─────────────────────────────────────────────

    /// <summary>
    /// Requirement 17.10 — GET /Cart with an empty session renders the
    /// "cart is empty" alert message.
    /// </summary>
    [Fact]
    public async Task CartPage_AuthenticatedGet_EmptyCart_RendersEmptyCartMessage()
    {
        // Arrange — no cart items in session (default for a fresh test client)
        var client = TestFactoryHelpers.CreateAuthenticatedClient(_factory);

        // Act
        var response = await client.GetAsync("/Cart");

        // Assert — authenticated users reach the cart page (200)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        // Requirement 17.10 — empty cart message from Cart.cshtml
        Assert.Contains("Your cart is empty", html);
    }

    /// <summary>
    /// GET /Cart without authentication redirects to the login page.
    /// </summary>
    [Fact]
    public async Task CartPage_UnauthenticatedGet_RedirectsToLogin()
    {
        // Arrange
        var client = TestFactoryHelpers.CreateAnonymousClient(_factory);

        // Act
        var response = await client.GetAsync("/Cart");

        // Assert — 302 redirect
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/Account/Login", location);
    }
}
