namespace PosWebApp.Infrastructure.Options;

/// <summary>
/// Strongly-typed binding for the <c>Auth</c> section in appsettings.json.
/// Controls session lifetime, account lockout policy, and cookie security.
/// Requirements: 25.6, 25.7
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Sliding session timeout for authenticated staff members (default 8 hours).
    /// Override via environment variable: <c>Auth__SessionTimeoutHours</c>.
    /// </summary>
    public int SessionTimeoutHours { get; set; } = 8;

    /// <summary>
    /// Maximum number of consecutive failed login attempts before account lockout.
    /// Default: 5.
    /// Override via environment variable: <c>Auth__MaxFailedAttempts</c>.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Duration in minutes that an account remains locked after exceeding
    /// <see cref="MaxFailedAttempts"/>. Default: 30 minutes.
    /// Override via environment variable: <c>Auth__LockoutMinutes</c>.
    /// </summary>
    public int LockoutMinutes { get; set; } = 30;
}
