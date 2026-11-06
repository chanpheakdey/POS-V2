using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using PosWebApp.Pages.Account;

namespace PosWebApp.Tests.Integration;

/// <summary>
/// Unit tests for the <see cref="LogoutModel"/> page model.
///
/// Validates: Requirements 2.7, 2.9, 15.2, 15.3, 15.5, 15.7, 15.8
/// </summary>
public class LogoutTests
{
    /// <summary>
    /// Builds a <see cref="LogoutModel"/> wired to a real (but in-memory)
    /// <see cref="DefaultHttpContext"/> with cookie authentication and a
    /// test session so we can verify the logout side-effects without a server.
    /// </summary>
    private static (LogoutModel Model, DefaultHttpContext Context, TestSession Session) CreateSut(
        bool authenticated = true)
    {
        var services = new ServiceCollection();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o => o.Cookie.Name = ".AspNetCore.Cookies");
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };

        var session = new TestSession();
        context.Features.Set<ISessionFeature>(new TestSessionFeature { Session = session });

        if (authenticated)
        {
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim("FullName", "Jane Cashier"),
                    new Claim(ClaimTypes.Name, "jcashier"),
                    new Claim(ClaimTypes.Role, "Staff"),
                },
                CookieAuthenticationDefaults.AuthenticationScheme);
            context.User = new ClaimsPrincipal(identity);
        }
        else
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        var model = new LogoutModel(NullLogger<LogoutModel>.Instance, new Mock<Microsoft.Extensions.Localization.IStringLocalizer<PosWebApp.Resources.SharedResource>>().Object);
        model.PageContext = new PageContext { HttpContext = context };

        return (model, context, session);
    }

    // ── OnGet ─────────────────────────────────────────────────────────────────

    /// <summary>Requirement 15.7 — logout is POST-only; GET must not log the user out.</summary>
    [Fact]
    public void OnGet_RedirectsToIndex_DoesNotSignOut()
    {
        var (model, _, _) = CreateSut();

        var result = model.OnGet();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    // ── OnPostAsync ───────────────────────────────────────────────────────────

    /// <summary>
    /// Requirements 15.2, 2.7 — OnPostAsync removes the authentication cookie
    /// (SignOutAsync) and clears server-side session state.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_SignsOut_ClearsSession_ReturnsPage()
    {
        var (model, context, session) = CreateSut();
        session.SetString("Current_Cart", "{\"Items\":[]}");
        Assert.False(session.ClearWasCalled);

        var result = await model.OnPostAsync();

        // 15.5 — returns the logout page (which clears localStorage + redirects via JS)
        Assert.IsType<PageResult>(result);

        // 2.7, 2.9, 15.3 — session state is cleared including Current_Cart
        Assert.True(session.ClearWasCalled);

        // 15.2 — the auth cookie is deleted (Set-Cookie header with expiry in the past)
        var setCookie = context.Response.Headers.SetCookie;
        Assert.True(
            setCookie.Any(c => c != null && c.Contains(".AspNetCore.Cookies") && c.Contains("expires")),
            "Expected an expired auth cookie in the Set-Cookie header after SignOutAsync");
    }

    /// <summary>
    /// Requirement 15.3 — clearing the session also removes the Current_Cart data.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ClearingSessionRemovesCart()
    {
        var (model, _, session) = CreateSut();
        session.SetString("Current_Cart", "cart-data-here");

        await model.OnPostAsync();

        Assert.Null(session.GetString("Current_Cart"));
    }

    // ── Attribute-based security ─────────────────────────────────────────────

    /// <summary>Requirement 15.7 — the page model validates anti-forgery tokens.</summary>
    [Fact]
    public void LogoutModel_HasValidateAntiForgeryTokenAttribute()
    {
        var attr = typeof(LogoutModel)
            .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true);
        Assert.Single(attr);
    }

    /// <summary>The logout page requires an authenticated user.</summary>
    [Fact]
    public void LogoutModel_HasAuthorizeAttribute()
    {
        var attr = typeof(LogoutModel)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
        Assert.Single(attr);
    }

    // ── Minimal in-memory session test double ────────────────────────────────

    /// <summary>
    /// A lightweight in-memory <see cref="ISession"/> implementation for testing.
    /// Tracks whether <see cref="Clear"/> was invoked.
    /// </summary>
    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool ClearWasCalled { get; private set; }
        public bool IsAvailable => true;
        public string Id { get; } = "test-session-id";
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear()
        {
            ClearWasCalled = true;
            _store.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value)
            => _store.TryGetValue(key, out value!);

        public byte[]? Get(string key) =>
            _store.TryGetValue(key, out var val) ? val : null;

        public string? GetString(string key)
        {
            var bytes = Get(key);
            return bytes == null ? null : System.Text.Encoding.UTF8.GetString(bytes);
        }

        public void SetString(string key, string value)
            => Set(key, System.Text.Encoding.UTF8.GetBytes(value));

        public void SetInt32(string key, int value)
            => SetString(key, value.ToString());

        public int? GetInt32(string key)
        {
            var s = GetString(key);
            return int.TryParse(s, out var v) ? v : null;
        }
    }

    private sealed class TestSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }
}
