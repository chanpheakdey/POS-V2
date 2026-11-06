using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using PosWebApp.Infrastructure.Exceptions;
using PosWebApp.Resources;
using PosWebApp.Services;

namespace PosWebApp.Pages.Account;

/// <summary>
/// Staff login page model. Authenticates against the Backend_Service
/// via <see cref="IServiceClient.LoginAsync"/> and issues a cookie
/// authentication ticket when the user has an allowed role.
/// </summary>
[EnableRateLimiting("authLogin")]
public class LoginModel : PageModel
{
    private readonly IServiceClient _serviceClient;
    private readonly ILogger<LoginModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    private static readonly HashSet<string> AllowedUserLevels =
        new(StringComparer.OrdinalIgnoreCase) { "Staff", "Manager", "Admin" };

    public LoginModel(
        IServiceClient serviceClient,
        ILogger<LoginModel> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _serviceClient = serviceClient;
        _logger        = logger;
        _localizer     = localizer;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    /// <summary>Return URL to redirect to after successful login (validated as local).</summary>
    [TempData]
    public string? ReturnUrl { get; set; }

    /// <summary>Stash return URL for the POST handler to read.</summary>
    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    /// <summary>
    /// Handles the login form POST. Validates credentials against the
    /// Backend_Service and issues a cookie on success.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await _serviceClient.LoginAsync(
                Input.UserName!,
                Input.Password!);

            // ── Locked out ────────────────────────────────────────────────
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    _localizer["Login_AccountLocked"]);
                _logger.LogWarning("Login attempt for {UserName} — account locked", Input.UserName);
                return Page();
            }

            // ── Invalid credentials ──────────────────────────────────────
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty,
                    result.ErrorMessage ?? _localizer["Login_InvalidCredentials"]);
                _logger.LogWarning("Login attempt for {UserName} — failed: {ErrorMessage}",
                    Input.UserName, result.ErrorMessage);
                return Page();
            }

            // ── Role check — reject even on IsSuccess if level not allowed ─
            if (!AllowedUserLevels.Contains(result.UserLevel))
            {
                ModelState.AddModelError(string.Empty,
                    "You do not have permission to access this application.");
                _logger.LogWarning(
                    "Login rejected for {UserName} — unauthorized UserLevel: {UserLevel}",
                    Input.UserName, result.UserLevel);
                return Page();
            }

            // ── Build claims principal ──────────────────────────────────
            var claims = new List<Claim>
            {
                new("UserID",    result.UserID.ToString()),
                new("UserLevel", result.UserLevel),
                new("FullName",  result.FullName),
                new(ClaimTypes.Name, Input.UserName!),
                new(ClaimTypes.Role, result.UserLevel),
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                // Persistent so the cookie survives browser restarts during a shift
                IsPersistent = true,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProps);

            _logger.LogInformation(
                "User {UserName} logged in successfully with level {UserLevel}",
                Input.UserName, result.UserLevel);

            // Prevent open-redirect — only redirect to local URLs
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToPage("/Index");
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex,
                "Service error during login for user {UserName}", Input.UserName);
            ModelState.AddModelError(string.Empty,
                "Unable to connect to the authentication service. Please try again later.");
            return Page();
        }
    }
}

/// <summary>
/// Input model for the login form with validation annotations.
/// </summary>
public class LoginInputModel
{
    [Required(ErrorMessage = "Please enter your user name.")]
    [Display(Name = "User Name")]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }
}
