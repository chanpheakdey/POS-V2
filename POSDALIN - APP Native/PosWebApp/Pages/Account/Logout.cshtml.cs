using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using PosWebApp.Resources;

namespace PosWebApp.Pages.Account;

/// <summary>
/// Staff logout page model. Removes the authentication cookie and clears all
/// server-side session state (including the Current_Cart) on POST.
///
/// Logout is POST-only with anti-forgery token validation (Requirement 15.7) to
/// prevent CSRF attacks — a simple GET link or image tag can never trigger it.
/// </summary>
[Authorize]
[ValidateAntiForgeryToken]
public class LogoutModel : PageModel
{
    private readonly ILogger<LogoutModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LogoutModel(
        ILogger<LogoutModel> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _logger    = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// GET is a no-op that bounces the user home. Logout must happen via POST
    /// (Requirement 15.7) so it can never be triggered accidentally by a GET.
    /// </summary>
    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    /// <summary>
    /// Handles the logout POST (form from the navbar). Removes the authentication
    /// cookie, clears all server-side session state, then renders a minimal page
    /// whose JavaScript clears browser localStorage and redirects to the login page.
    ///
    /// Requirements: 2.7, 2.9, 15.2, 15.3, 15.4, 15.5, 15.7
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var userName = User.Identity?.Name;

        // 15.2 — remove the authentication cookie
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        // 2.7, 2.9, 15.3 — clear all server-side session state including Current_Cart
        HttpContext.Session.Clear();

        _logger.LogInformation("User {UserName} logged out", userName);

        // 15.4 + 15.5 — the view clears localStorage then redirects to login
        return Page();
    }
}
