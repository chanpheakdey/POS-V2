using System.Security.Claims;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PosWebApp.Models.DTOs;
using PosWebApp.Pages.Account;
using PosWebApp.Services;

namespace PosWebApp.Tests.Integration;

/// <summary>
/// Unit tests for the <see cref="LoginModel"/> page model.
///
/// Validates: Requirements 1.2, 1.4, 1.6, 1.7
/// </summary>
public class AuthenticationTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="LoginModel"/> wired to a real (but in-memory)
    /// <see cref="DefaultHttpContext"/> so <see cref="HttpContext.SignInAsync"/> works
    /// without a running server.
    /// </summary>
    private static (LoginModel Model, Mock<IServiceClient> ServiceClientMock, DefaultHttpContext Context)
        CreateSut()
    {
        var services = new ServiceCollection();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o => o.Cookie.Name = ".AspNetCore.Cookies");
        services.AddLogging();
        // IUrlHelperFactory is needed when the handler calls Url.IsLocalUrl()
        services.AddMvc();
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };

        var serviceClientMock = new Mock<IServiceClient>();

        // Set up the localizer with actual English resource values so that tests can
        // assert on the displayed messages without running the real resource pipeline.
        var resourceValues = new Dictionary<string, string>
        {
            ["Login_AccountLocked"]    = "Your user account has been locked! Please contact IT",
            ["Login_InvalidCredentials"] = "Invalid username or password.",
        };
        var localizerMock = new Mock<Microsoft.Extensions.Localization.IStringLocalizer<PosWebApp.Resources.SharedResource>>();
        localizerMock
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) =>
            {
                var value = resourceValues.TryGetValue(key, out var v) ? v : key;
                return new Microsoft.Extensions.Localization.LocalizedString(key, value);
            });
        localizerMock
            .Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] _) =>
            {
                var value = resourceValues.TryGetValue(key, out var v) ? v : key;
                return new Microsoft.Extensions.Localization.LocalizedString(key, value);
            });

        var model = new LoginModel(serviceClientMock.Object, NullLogger<LoginModel>.Instance, localizerMock.Object);

        // ActionDescriptor and RouteData are required by UrlHelperFactory (used when
        // the handler calls Url.IsLocalUrl()). Without them a NullReferenceException
        // is thrown inside UrlHelperBase..ctor.
        var actionDescriptor = new Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor();
        var routeData = new Microsoft.AspNetCore.Routing.RouteData();
        var actionContext = new ActionContext(context, routeData, actionDescriptor);
        model.PageContext = new PageContext(actionContext);

        // Supply a valid model state by populating the bound input property directly
        model.Input = new LoginInputModel
        {
            UserName = "testuser",
            Password = "testpassword",
        };

        return (model, serviceClientMock, context);
    }

    /// <summary>
    /// Creates a <see cref="LoginResult"/> representing a successful login
    /// with the given <paramref name="userLevel"/>.
    /// </summary>
    private static LoginResult SuccessResult(string userLevel = "Staff") =>
        new LoginResult(
            IsSuccess: true,
            IsLockedOut: false,
            ErrorMessage: null,
            UserID: 42,
            UserLevel: userLevel,
            FullName: "Test User");

    /// <summary>
    /// Creates a <see cref="LoginResult"/> representing a failed login.
    /// </summary>
    private static LoginResult FailureResult(string errorMessage = "Password is incorrect!") =>
        new LoginResult(
            IsSuccess: false,
            IsLockedOut: false,
            ErrorMessage: errorMessage,
            UserID: 0,
            UserLevel: string.Empty,
            FullName: string.Empty);

    /// <summary>
    /// Creates a <see cref="LoginResult"/> representing a locked-out account.
    /// </summary>
    private static LoginResult LockedOutResult() =>
        new LoginResult(
            IsSuccess: false,
            IsLockedOut: true,
            ErrorMessage: null,
            UserID: 0,
            UserLevel: string.Empty,
            FullName: string.Empty);

    // ── Success / redirect ────────────────────────────────────────────────────

    /// <summary>
    /// Requirement 1.2, 1.3 — A successful login with UserLevel=Staff redirects to /Index.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ValidLogin_Staff_RedirectsToIndex()
    {
        var (model, mock, _) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(SuccessResult("Staff"));

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    /// <summary>
    /// Requirement 1.2, 1.3 — A successful login with UserLevel=Manager redirects to /Index.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ValidLogin_Manager_RedirectsToIndex()
    {
        var (model, mock, _) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(SuccessResult("Manager"));

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    /// <summary>
    /// Requirement 1.2, 1.3 — A successful login with UserLevel=Admin redirects to /Index.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ValidLogin_Admin_RedirectsToIndex()
    {
        var (model, mock, _) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(SuccessResult("Admin"));

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    /// <summary>
    /// Requirement 1.8 — On successful login, the authentication cookie is written
    /// (SignInAsync was called).
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ValidLogin_IssuesAuthCookie()
    {
        var (model, mock, context) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(SuccessResult("Staff"));

        await model.OnPostAsync();

        // SignInAsync writes Set-Cookie headers
        var setCookie = context.Response.Headers.SetCookie;
        Assert.True(
            setCookie.Any(c => c != null && c.Contains(".AspNetCore.Cookies")),
            "Expected an auth cookie in the Set-Cookie response header after successful login");
    }

    // ── IsSuccess = false ─────────────────────────────────────────────────────

    /// <summary>
    /// Requirement 1.4 — When credentials are invalid (IsSuccess=false), OnPostAsync
    /// returns PageResult (stays on login page) with a model error.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_InvalidCredentials_ReturnsPage_WithModelError()
    {
        var (model, mock, _) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(FailureResult("Password is incorrect!"));

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(string.Empty));
        Assert.Contains("Password is incorrect!", model.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage));
    }

    /// <summary>
    /// Requirement 1.4 — The ModelState error message matches the ErrorMessage
    /// returned by the service when credentials fail.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_InvalidCredentials_ModelError_ReflectsServiceErrorMessage()
    {
        var (model, mock, _) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(FailureResult("User name is incorrect!"));

        await model.OnPostAsync();

        var errors = model.ModelState[string.Empty]!.Errors;
        Assert.Contains("User name is incorrect!", errors.Select(e => e.ErrorMessage));
    }

    /// <summary>
    /// Requirement 1.4 — No auth cookie is issued when credentials are invalid.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_InvalidCredentials_DoesNotIssueAuthCookie()
    {
        var (model, mock, context) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(FailureResult());

        await model.OnPostAsync();

        var setCookie = context.Response.Headers.SetCookie;
        Assert.False(
            setCookie.Any(c => c != null && c.Contains(".AspNetCore.Cookies")),
            "No auth cookie should be issued when login fails");
    }

    // ── IsLockedOut = true ────────────────────────────────────────────────────

    /// <summary>
    /// Requirement 1.6 — When the account is locked, OnPostAsync returns PageResult
    /// and adds the lockout message to ModelState.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_LockedOut_ReturnsPage_WithLockoutMessage()
    {
        var (model, mock, _) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(LockedOutResult());

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        var errors = model.ModelState[string.Empty]!.Errors;
        Assert.Contains(
            "Your user account has been locked! Please contact IT",
            errors.Select(e => e.ErrorMessage));
    }

    /// <summary>
    /// Requirement 1.6 — No auth cookie is issued when the account is locked.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_LockedOut_DoesNotIssueAuthCookie()
    {
        var (model, mock, context) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(LockedOutResult());

        await model.OnPostAsync();

        var setCookie = context.Response.Headers.SetCookie;
        Assert.False(
            setCookie.Any(c => c != null && c.Contains(".AspNetCore.Cookies")),
            "No auth cookie should be issued for a locked account");
    }

    // ── UserLevel outside allowed set ─────────────────────────────────────────

    /// <summary>
    /// Requirement 1.7 — Even when IsSuccess=true, an unrecognised UserLevel is rejected.
    /// </summary>
    [Theory]
    [InlineData("Customer")]
    [InlineData("Guest")]
    [InlineData("SuperAdmin")]
    [InlineData("")]
    [InlineData("STAFF")]   // Wrong casing should still match due to OrdinalIgnoreCase — expect PASS
    public async Task OnPostAsync_DisallowedUserLevel_ReturnsPage_WithPermissionError(string userLevel)
    {
        // UserLevel values in AllowedUserLevels (case-insensitive): Staff, Manager, Admin
        // The ones expected to be rejected are those NOT in the set.
        var isAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Staff", "Manager", "Admin" }
            .Contains(userLevel);

        var (model, mock, _) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(new LoginResult(
                IsSuccess: true,
                IsLockedOut: false,
                ErrorMessage: null,
                UserID: 1,
                UserLevel: userLevel,
                FullName: "Unauthorized User"));

        var result = await model.OnPostAsync();

        if (isAllowed)
        {
            // Case-insensitive match — "STAFF" should be accepted
            Assert.IsType<RedirectToPageResult>(result);
        }
        else
        {
            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            var errors = model.ModelState[string.Empty]!.Errors;
            Assert.Contains(
                "You do not have permission to access this application.",
                errors.Select(e => e.ErrorMessage));
        }
    }

    /// <summary>
    /// Requirement 1.7 — No auth cookie is issued when UserLevel is not in the allowed set.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_UnauthorizedUserLevel_DoesNotIssueAuthCookie()
    {
        var (model, mock, context) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(new LoginResult(
                IsSuccess: true,
                IsLockedOut: false,
                ErrorMessage: null,
                UserID: 1,
                UserLevel: "Customer",
                FullName: "Unauthorized User"));

        await model.OnPostAsync();

        var setCookie = context.Response.Headers.SetCookie;
        Assert.False(
            setCookie.Any(c => c != null && c.Contains(".AspNetCore.Cookies")),
            "No auth cookie should be issued for an unauthorised UserLevel");
    }

    // ── Invalid ModelState ────────────────────────────────────────────────────

    /// <summary>
    /// If ModelState is invalid (e.g. empty username), the handler returns Page()
    /// without calling LoginAsync at all.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_InvalidModelState_ReturnsPage_WithoutCallingService()
    {
        var (model, mock, _) = CreateSut();
        model.ModelState.AddModelError("Input.UserName", "Please enter your user name.");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        mock.Verify(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── ReturnUrl ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Requirement 1.9 — A local return URL is honoured after successful login.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ValidLogin_WithLocalReturnUrl_RedirectsToReturnUrl()
    {
        var (model, mock, context) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(SuccessResult("Staff"));

        // Url.IsLocalUrl needs a Request with a Host; set it up minimally
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var result = await model.OnPostAsync(returnUrl: "/Cart");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/Cart", redirect.Url);
    }

    /// <summary>
    /// An external (non-local) return URL is ignored and the default redirect to /Index is used.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ValidLogin_WithExternalReturnUrl_RedirectsToIndex()
    {
        var (model, mock, context) = CreateSut();
        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(SuccessResult("Staff"));

        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");

        var result = await model.OnPostAsync(returnUrl: "https://evil.example.com/steal");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Property-Based Tests — Property 1: Role-Based Authentication
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Property-based tests for <strong>Property 1: Role-Based Authentication</strong>.
///
/// For any generated combination of <c>(IsSuccess, IsLockedOut, UserLevel)</c>,
/// the <see cref="LoginModel"/> SHALL issue an auth cookie if and only if
/// <c>IsSuccess = true AND IsLockedOut = false AND UserLevel IN ('Staff','Manager','Admin')</c>.
/// All other combinations must result in no cookie being written.
///
/// The generator produces the full boolean-cross-string space, with the three valid
/// user levels sampled at a higher frequency so that both branches are exercised
/// thoroughly across the 200 minimum iterations.
///
/// **Validates: Requirements 1.2, 1.7**
/// </summary>
public class RoleBasedAuthenticationPropertyTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly HashSet<string> ValidLevels =
        new(StringComparer.OrdinalIgnoreCase) { "Staff", "Manager", "Admin" };

    /// <summary>
    /// Determines whether a <see cref="LoginResult"/> should produce an auth cookie.
    /// A cookie is issued iff IsSuccess=true AND IsLockedOut=false AND
    /// UserLevel is one of Staff / Manager / Admin (case-insensitive).
    /// </summary>
    private static bool ShouldIssueCookie(LoginResult r) =>
        r.IsSuccess && !r.IsLockedOut && ValidLevels.Contains(r.UserLevel);

    /// <summary>
    /// Builds a <see cref="LoginModel"/> wired to a real (but in-memory)
    /// <see cref="DefaultHttpContext"/> so <c>HttpContext.SignInAsync</c> works
    /// without a running server.  Returns the model, a mock service client, and
    /// the context so the Set-Cookie header can be inspected.
    /// </summary>
    private static (LoginModel Model, Mock<IServiceClient> ServiceClientMock, DefaultHttpContext Context)
        CreateSut()
    {
        var services = new ServiceCollection();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o => o.Cookie.Name = ".AspNetCore.Cookies");
        services.AddLogging();
        services.AddMvc();
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };

        var serviceClientMock = new Mock<IServiceClient>();

        // Set up the localizer so that all key lookups return a non-null LocalizedString.
        // Without this, ModelState.AddModelError throws ArgumentNullException when the
        // page model calls _localizer["Login_AccountLocked"] (or similar) and the mock
        // returns default(LocalizedString), whose Value property is null.
        var localizerMock = new Mock<Microsoft.Extensions.Localization.IStringLocalizer<PosWebApp.Resources.SharedResource>>();
        localizerMock
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) =>
                new Microsoft.Extensions.Localization.LocalizedString(key, key));
        localizerMock
            .Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] _) =>
                new Microsoft.Extensions.Localization.LocalizedString(key, key));

        var model = new LoginModel(
            serviceClientMock.Object,
            NullLogger<LoginModel>.Instance,
            localizerMock.Object);

        model.PageContext = new PageContext { HttpContext = context };
        model.Input = new LoginInputModel
        {
            UserName = "testuser",
            Password = "testpassword",
        };

        return (model, serviceClientMock, context);
    }

    // ── Arbitrary / Generator ─────────────────────────────────────────────────

    /// <summary>
    /// Generates a <see cref="LoginResult"/> covering all combinations of
    /// <c>(IsSuccess, IsLockedOut, UserLevel)</c>.
    ///
    /// <para>
    /// User level is chosen from a pool that mixes the three valid levels
    /// (<c>Staff</c>, <c>Manager</c>, <c>Admin</c>) with a collection of
    /// invalid / edge-case levels so that both the "should succeed" and the
    /// "should be rejected" branches are exercised during the 200-run sweep.
    /// </para>
    /// </summary>
    public static Arbitrary<LoginResult> LoginResultArb()
    {
        // User-level candidates: valid levels (3) + invalid levels (5)
        var userLevels = new[]
        {
            "Staff", "Manager", "Admin",          // valid
            "Customer", "Guest", "SuperAdmin",    // invalid — not in allowed set
            "",                                   // invalid — empty string
            "staff",                              // lowercase — accepted (OrdinalIgnoreCase)
        };

        var gen =
            // IsSuccess: true / false
            Gen.Elements(true, false)
               .SelectMany(isSuccess =>
                   // IsLockedOut: true / false
                   Gen.Elements(true, false)
                      .SelectMany(isLockedOut =>
                          // UserLevel: one of the candidate strings
                          Gen.Elements(userLevels)
                             .Select(userLevel => new LoginResult(
                                 IsSuccess:    isSuccess,
                                 IsLockedOut:  isLockedOut,
                                 ErrorMessage: isSuccess ? null : "Password is incorrect!",
                                 UserID:       isSuccess ? 42 : 0,
                                 UserLevel:    userLevel,
                                 FullName:     isSuccess ? "Test User" : string.Empty))));

        return gen.ToArbitrary();
    }

    // ── Property ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <strong>Property 1: Role-Based Authentication</strong>
    ///
    /// For any generated <see cref="LoginResult"/>, the auth cookie is written if and
    /// only if <c>IsSuccess=true AND IsLockedOut=false AND
    /// UserLevel IN ('Staff','Manager','Admin')</c>.
    ///
    /// <list type="bullet">
    ///   <item>When all three conditions hold → expect <c>Set-Cookie</c> header present</item>
    ///   <item>Any other combination → expect NO <c>Set-Cookie</c> header</item>
    /// </list>
    ///
    /// **Validates: Requirements 1.2, 1.7**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(RoleBasedAuthenticationPropertyTests) }, MaxTest = 200)]
    public Property Property1_CookieIssuedIffAllAuthConditionsMet(LoginResult loginResult)
    {
        // ── Arrange ──────────────────────────────────────────────────────────
        var (model, mock, context) = CreateSut();

        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(loginResult);

        // ── Act ───────────────────────────────────────────────────────────────
        // Run synchronously inside the property body — FsCheck does not support
        // async properties natively; Task.GetAwaiter().GetResult() is the
        // standard pattern for synchronous execution of async Page handlers.
        model.OnPostAsync().GetAwaiter().GetResult();

        // ── Assert ────────────────────────────────────────────────────────────
        bool cookiePresent = context.Response.Headers.SetCookie
            .Any(c => c != null && c.Contains(".AspNetCore.Cookies"));

        bool expectedCookie = ShouldIssueCookie(loginResult);

        return (cookiePresent == expectedCookie).Label(
            $"IsSuccess={loginResult.IsSuccess}, " +
            $"IsLockedOut={loginResult.IsLockedOut}, " +
            $"UserLevel='{loginResult.UserLevel}' → " +
            $"expectedCookie={expectedCookie}, gotCookie={cookiePresent}");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Property-Based Tests — Property 2: Account Lockout
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Property-based tests for <strong>Property 2: Account Lockout</strong>.
///
/// For any generated <see cref="LoginResult"/> where <c>IsLockedOut=true</c>,
/// the <see cref="LoginModel"/> SHALL:
/// <list type="bullet">
///   <item>Always display the lockout message regardless of <c>IsSuccess</c>,
///         <c>UserLevel</c>, or <c>ErrorMessage</c></item>
///   <item>Never issue an authentication cookie</item>
/// </list>
///
/// The generator fixes <c>IsLockedOut=true</c> and varies all other fields
/// (including combinations where <c>IsSuccess=true</c> and a valid <c>UserLevel</c>
/// like "Staff") to prove the lockout check is unconditional.
///
/// **Validates: Requirements 1.5, 1.6**
/// </summary>
public class AccountLockoutPropertyTests
{
    private const string LockoutMessage = "Your user account has been locked! Please contact IT";

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="LoginModel"/> wired to a real (but in-memory)
    /// <see cref="DefaultHttpContext"/> so <c>HttpContext.SignInAsync</c> works
    /// without a running server.
    /// </summary>
    private static (LoginModel Model, Mock<IServiceClient> ServiceClientMock, DefaultHttpContext Context)
        CreateSut()
    {
        var services = new ServiceCollection();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o => o.Cookie.Name = ".AspNetCore.Cookies");
        services.AddLogging();
        services.AddMvc();
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };

        var serviceClientMock = new Mock<IServiceClient>();

        // Localizer mock: the catch-all is registered first; the specific
        // "Login_AccountLocked" setup is registered last so Moq (which evaluates
        // setups in reverse registration order) matches the specific entry first.
        var localizerMock = new Mock<Microsoft.Extensions.Localization.IStringLocalizer<PosWebApp.Resources.SharedResource>>();
        localizerMock
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) =>
                new Microsoft.Extensions.Localization.LocalizedString(key, key));
        localizerMock
            .Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] _) =>
                new Microsoft.Extensions.Localization.LocalizedString(key, key));
        // Specific override — registered last so it takes priority over the catch-all.
        localizerMock
            .Setup(l => l["Login_AccountLocked"])
            .Returns(new Microsoft.Extensions.Localization.LocalizedString(
                "Login_AccountLocked", LockoutMessage));

        var model = new LoginModel(
            serviceClientMock.Object,
            NullLogger<LoginModel>.Instance,
            localizerMock.Object);

        model.PageContext = new PageContext { HttpContext = context };
        model.Input = new LoginInputModel
        {
            UserName = "testuser",
            Password = "testpassword",
        };

        return (model, serviceClientMock, context);
    }

    // ── Arbitrary / Generator ─────────────────────────────────────────────────

    /// <summary>
    /// Generates a <see cref="LoginResult"/> with <c>IsLockedOut=true</c> and
    /// all other fields varied to cover every combination the property must hold for.
    ///
    /// <para>
    /// User-level candidates include the three normally-valid levels
    /// (<c>Staff</c>, <c>Manager</c>, <c>Admin</c>) as well as invalid ones
    /// (<c>Customer</c>, <c>Guest</c>, empty string) so the test demonstrates
    /// the lockout check fires unconditionally, even when the user level would
    /// otherwise pass the role check.
    /// </para>
    /// </summary>
    public static Arbitrary<LoginResult> LockedOutLoginResultArb()
    {
        // All user-level candidates — valid and invalid — to prove lockout is
        // unconditional with respect to UserLevel.
        var userLevels = new[]
        {
            "Staff", "Manager", "Admin",         // would normally be accepted
            "Customer", "Guest", "SuperAdmin",   // never accepted
            "",                                  // empty — never accepted
        };

        // Error messages: null or a non-empty string
        var errorMessages = new string?[]
        {
            null,
            "Password is incorrect!",
            "User name is incorrect!",
            "Account locked by administrator.",
        };

        var gen =
            // IsSuccess: true / false — lockout must override even IsSuccess=true
            Gen.Elements(true, false)
               .SelectMany(isSuccess =>
                   Gen.Elements(userLevels)
                      .SelectMany(userLevel =>
                          Gen.Elements(errorMessages)
                             .Select(errorMessage => new LoginResult(
                                 IsSuccess:    isSuccess,
                                 IsLockedOut:  true,           // always true — the invariant under test
                                 ErrorMessage: errorMessage,
                                 UserID:       isSuccess ? 42 : 0,
                                 UserLevel:    userLevel,
                                 FullName:     isSuccess ? "Test User" : string.Empty))));

        return gen.ToArbitrary();
    }

    // ── Property ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <strong>Property 2: Account Lockout</strong>
    ///
    /// For any <see cref="LoginResult"/> with <c>IsLockedOut=true</c>, the Login page:
    /// <list type="number">
    ///   <item>Returns <see cref="PageResult"/> (stays on the login page)</item>
    ///   <item>Adds the lockout message to <c>ModelState</c></item>
    ///   <item>Does NOT issue an authentication cookie</item>
    /// </list>
    ///
    /// This holds regardless of <c>IsSuccess</c>, <c>UserLevel</c>, or
    /// <c>ErrorMessage</c> values, demonstrating the lockout check is
    /// unconditional and takes priority over all other result fields.
    ///
    /// **Validates: Requirements 1.5, 1.6**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(AccountLockoutPropertyTests) }, MaxTest = 200)]
    public Property Property2_LockoutAlwaysShowsLockoutMessage(LoginResult loginResult)
    {
        // ── Arrange ──────────────────────────────────────────────────────────
        var (model, mock, context) = CreateSut();

        mock.Setup(s => s.LoginAsync("testuser", "testpassword"))
            .ReturnsAsync(loginResult);

        // ── Act ───────────────────────────────────────────────────────────────
        // FsCheck does not support async properties; run synchronously.
        var actionResult = model.OnPostAsync().GetAwaiter().GetResult();

        // ── Assert ────────────────────────────────────────────────────────────

        // 1. Must stay on the page (PageResult)
        bool isPageResult = actionResult is PageResult;

        // 2. ModelState must contain the lockout message
        bool hasLockoutMessage = model.ModelState.ContainsKey(string.Empty)
            && model.ModelState[string.Empty]!.Errors
                    .Any(e => e.ErrorMessage == LockoutMessage);

        // 3. No auth cookie must be issued
        bool noCookie = !context.Response.Headers.SetCookie
            .Any(c => c != null && c.Contains(".AspNetCore.Cookies"));

        bool holds = isPageResult && hasLockoutMessage && noCookie;

        return holds.Label(
            $"IsSuccess={loginResult.IsSuccess}, " +
            $"UserLevel='{loginResult.UserLevel}', " +
            $"ErrorMessage='{loginResult.ErrorMessage}' → " +
            $"isPageResult={isPageResult}, " +
            $"hasLockoutMessage={hasLockoutMessage}, " +
            $"noCookie={noCookie}");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Razor Page Rendering Tests — Task 28.2
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Integration tests that verify Razor Page HTML rendering using
/// <see cref="WebApplicationFactory{TEntryPoint}"/> (<see cref="SaleFlowTests.PosWebAppFactory"/>).
///
/// Validates: Requirements 1.1, 17.10
/// </summary>
public class RazorPageRenderingLegacyTests : IClassFixture<SaleFlowTests.PosWebAppFactory>
{
    private readonly SaleFlowTests.PosWebAppFactory _factory;

    private const string TestUsername = "cashier01";
    private const string TestPassword = "Password1!";

    public RazorPageRenderingLegacyTests(SaleFlowTests.PosWebAppFactory factory)
    {
        _factory = factory;

        // Login mock: returns a successful Staff result.
        _factory.ServiceClientMock
            .Setup(s => s.LoginAsync(TestUsername, TestPassword))
            .ReturnsAsync(new PosWebApp.Models.DTOs.LoginResult(
                IsSuccess:    true,
                IsLockedOut:  false,
                ErrorMessage: null,
                UserID:       1,
                UserLevel:    "Staff",
                FullName:     "Rendering Test User"));

        // Catalog mock: returns an empty list so the home page renders without error.
        _factory.CatalogMock
            .Setup(s => s.GetCategoriesAsync())
            .ReturnsAsync(new List<PosWebApp.Models.DTOs.CategoryDto>());

        _factory.CatalogMock
            .Setup(s => s.GetGroupsAsync(It.IsAny<int>()))
            .ReturnsAsync(System.Array.Empty<PosWebApp.Models.DTOs.GroupDto>());

        _factory.CatalogMock
            .Setup(s => s.GetItemsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(System.Array.Empty<PosWebApp.Models.DTOs.ItemDto>());
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an <see cref="HttpClient"/> that automatically tracks cookies
    /// across redirects, emulating browser behaviour.
    /// </summary>
    private HttpClient CreateClientWithCookies()
    {
        var cookieContainer = new System.Net.CookieContainer();
        var handler = new RedirectCookieHandler(_factory.Server.CreateHandler(), cookieContainer);
        return new HttpClient(handler) { BaseAddress = _factory.Server.BaseAddress };
    }

    /// <summary>Thin cookie-tracking delegating handler (mirrors the one in SaleFlowTests).</summary>
    private sealed class RedirectCookieHandler : System.Net.Http.DelegatingHandler
    {
        private readonly System.Net.CookieContainer _cookies;
        private readonly Uri _baseAddress;

        public RedirectCookieHandler(System.Net.Http.HttpMessageHandler inner,
                                     System.Net.CookieContainer cookies)
            : base(inner)
        {
            _cookies     = cookies;
            _baseAddress = new Uri("http://localhost/");
        }

        protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(
            System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cookieHeader = _cookies.GetCookieHeader(request.RequestUri ?? _baseAddress);
            if (!string.IsNullOrEmpty(cookieHeader))
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
                foreach (var c in setCookies)
                    _cookies.SetCookies(request.RequestUri ?? _baseAddress, c);

            // Follow redirects (302/301/303) up to 10 hops.
            int hops = 0;
            while ((response.StatusCode == System.Net.HttpStatusCode.Found      ||
                    response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                    response.StatusCode == System.Net.HttpStatusCode.SeeOther)  &&
                   response.Headers.Location != null && hops++ < 10)
            {
                var location = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(request.RequestUri ?? _baseAddress, response.Headers.Location);

                var redirectReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, location);
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
    /// Logs in via POST /Account/Login and returns the authenticated client.
    /// </summary>
    private async Task<HttpClient> LoginAsync(HttpClient client)
    {
        await client.GetAsync("/Account/Login");

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.UserName", TestUsername),
            new KeyValuePair<string, string>("Input.Password", TestPassword),
            new KeyValuePair<string, string>("__RequestVerificationToken", "test-request-token"),
        });

        var response = await client.PostAsync("/Account/Login", form);
        Assert.True(
            response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Found,
            $"Login POST returned unexpected status: {response.StatusCode}");

        return client;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Requirement 1.1 — GET /Account/Login renders a form with username and
    /// password input fields so that staff can enter their credentials.
    ///
    /// The Razor Page uses <c>asp-for="Input.UserName"</c> and
    /// <c>asp-for="Input.Password"</c>, which the tag helper expands to
    /// <c>name="Input.UserName"</c> and <c>name="Input.Password"</c> in the
    /// rendered HTML.
    ///
    /// **Validates: Requirements 1.1**
    /// </summary>
    [Fact]
    public async Task LoginPage_Get_RendersUsernameAndPasswordInputFields()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Account/Login");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        // The asp-for tag helper renders name="Input.UserName" / name="Input.Password"
        Assert.Contains("name=\"Input.UserName\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"Input.Password\"", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Requirement 17.10 — When the cart is empty, GET /Cart renders the
    /// "Your cart is empty" informational message so staff know the cart
    /// has no items before proceeding to checkout.
    ///
    /// **Validates: Requirements 17.10**
    /// </summary>
    [Fact]
    public async Task CartPage_WhenCartEmpty_RendersEmptyCartMessage()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client);

        // GET /Cart without adding any items — the cart session is empty.
        var response = await client.GetAsync("/Cart");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        // Cart.cshtml renders: "Your cart is empty." inside an alert div.
        Assert.Contains("cart is empty", html, StringComparison.OrdinalIgnoreCase);
    }
}
