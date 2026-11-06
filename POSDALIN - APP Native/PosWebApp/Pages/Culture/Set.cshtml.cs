using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PosWebApp.Pages.Culture;

/// <summary>
/// Handles the language-selection POST from the navbar language dropdown.
///
/// The language selector in _NavbarPartial.cshtml POSTs to /Culture/Set with
/// two fields: <c>culture</c> (e.g. "en-US" or "km-KH") and <c>returnUrl</c>.
///
/// This page model:
/// 1. Validates the requested culture against the supported set.
/// 2. Writes the .AspNetCore.Culture cookie so RequestLocalizationMiddleware
///    picks it up on all subsequent requests (Req 13.4).
/// 3. Redirects back to <c>returnUrl</c> (or "/" when absent) so the user
///    sees the page they were on, now rendered in the selected language.
///
/// <b>Validates: Requirements 13.3, 13.4, 13.5</b>
/// </summary>
public class SetModel : PageModel
{
    private static readonly HashSet<string> SupportedCultures =
        new(StringComparer.OrdinalIgnoreCase) { "en-US", "km-KH" };

    /// <summary>The BCP-47 culture tag to apply (e.g. "en-US", "km-KH").</summary>
    [BindProperty]
    public string Culture { get; set; } = "en-US";

    /// <summary>The URL to redirect to after storing the cookie.</summary>
    [BindProperty]
    public string ReturnUrl { get; set; } = "/";

    /// <summary>
    /// GET is not used — redirect home if someone lands here directly.
    /// </summary>
    public IActionResult OnGet() => LocalRedirect("/");

    /// <summary>
    /// Stores the culture preference as a <c>.AspNetCore.Culture</c> cookie
    /// and redirects back to the originating page.
    /// </summary>
    public IActionResult OnPost()
    {
        // Guard: only accept known cultures to prevent open-redirect via cookie abuse.
        if (!SupportedCultures.Contains(Culture))
        {
            Culture = "en-US";
        }

        // Sanitise returnUrl to local paths only (prevents open redirect).
        var safeReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";

        // Write the .AspNetCore.Culture cookie.
        // CookieRequestCultureProvider reads this cookie on every request (Req 13.4).
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(Culture)),
            new CookieOptions
            {
                Expires     = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,          // GDPR: functional cookie — no consent needed
                SameSite    = SameSiteMode.Strict,
                HttpOnly    = false,         // Allow JavaScript to read for UI purposes
                Secure      = false,         // Allow on HTTP in development; prod enforced by HTTPS middleware
            });

        return LocalRedirect(safeReturnUrl);
    }
}
