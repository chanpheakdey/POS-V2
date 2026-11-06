using Microsoft.Extensions.Options;

namespace PosWebApp.Infrastructure.Options;

// ─────────────────────────────────────────────────────────────────────────────
//  Startup-time option validators
//  Each validator is registered with AddSingleton<IValidateOptions<T>>() and is
//  invoked the first time the options are resolved from DI (which we force at
//  startup by calling ValidateOnStart()).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Validates <see cref="DatabaseOptions"/> at application startup.
/// Fails fast if database config is invalid (Requirement 25.1).
/// </summary>
public sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var failures = new List<string>();

        if (options.CommandTimeoutSeconds <= 0)
            failures.Add("Database:CommandTimeoutSeconds must be greater than 0.");

        if (options.RetryCount < 0)
            failures.Add("Database:RetryCount must be 0 or greater.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validates <see cref="CacheOptions"/> at application startup (Requirement 25.4).
/// </summary>
public sealed class CacheOptionsValidator : IValidateOptions<CacheOptions>
{
    public ValidateOptionsResult Validate(string? name, CacheOptions options)
    {
        var failures = new List<string>();

        if (options.CatalogSlidingExpirationMinutes <= 0)
            failures.Add("Cache:CatalogSlidingExpirationMinutes must be greater than 0.");

        if (options.ImageAbsoluteExpirationHours <= 0)
            failures.Add("Cache:ImageAbsoluteExpirationHours must be greater than 0.");

        if (options.UseRedis && string.IsNullOrWhiteSpace(options.RedisConnectionString))
            failures.Add("Cache:RedisConnectionString must not be empty when Cache:UseRedis is true. " +
                         "Supply it via Secret Manager (development) or the " +
                         "Cache__RedisConnectionString environment variable (production).");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validates <see cref="AuthOptions"/> at application startup (Requirement 25.6).
/// </summary>
public sealed class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        var failures = new List<string>();

        if (options.SessionTimeoutHours <= 0)
            failures.Add("Auth:SessionTimeoutHours must be greater than 0.");

        if (options.MaxFailedAttempts <= 0)
            failures.Add("Auth:MaxFailedAttempts must be greater than 0.");

        if (options.LockoutMinutes <= 0)
            failures.Add("Auth:LockoutMinutes must be greater than 0.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validates <see cref="ImageOptions"/> at application startup (Requirement 25.8).
/// </summary>
public sealed class ImageOptionsValidator : IValidateOptions<ImageOptions>
{
    public ValidateOptionsResult Validate(string? name, ImageOptions options)
    {
        var failures = new List<string>();

        if (options.CategorySize.Width <= 0 || options.CategorySize.Height <= 0)
            failures.Add("Image:CategorySize Width and Height must be greater than 0.");

        if (options.ItemThumbnailSize.Width <= 0 || options.ItemThumbnailSize.Height <= 0)
            failures.Add("Image:ItemThumbnailSize Width and Height must be greater than 0.");

        if (options.MaxConcurrentProcessing <= 0)
            failures.Add("Image:MaxConcurrentProcessing must be greater than 0.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}