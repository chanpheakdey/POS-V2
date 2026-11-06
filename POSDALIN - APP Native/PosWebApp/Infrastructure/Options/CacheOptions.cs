namespace PosWebApp.Infrastructure.Options;

/// <summary>
/// Strongly-typed binding for the <c>Cache</c> section in appsettings.json.
/// Controls catalog/image sliding expiration windows and optional Redis backing.
/// Requirements: 25.4, 25.5
/// </summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Sliding expiration in minutes for catalog entries (categories, groups, items).
    /// Default: 60 minutes.
    /// Override via environment variable: <c>Cache__CatalogSlidingExpirationMinutes</c>.
    /// </summary>
    public int CatalogSlidingExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Absolute expiration in hours for optimized image entries.
    /// Default: 24 hours.
    /// Override via environment variable: <c>Cache__ImageAbsoluteExpirationHours</c>.
    /// </summary>
    public int ImageAbsoluteExpirationHours { get; set; } = 24;

    /// <summary>
    /// When <c>true</c>, a Redis distributed cache is used; otherwise an in-memory
    /// distributed cache is used. Default: <c>false</c>.
    /// Override via environment variable: <c>Cache__UseRedis</c>.
    /// </summary>
    public bool UseRedis { get; set; } = false;

    /// <summary>
    /// Redis connection string. Required when <see cref="UseRedis"/> is <c>true</c>.
    /// Provide via Secret Manager in development or as a container environment variable
    /// in production — do NOT hardcode this value.
    /// Override via environment variable: <c>Cache__RedisConnectionString</c>.
    /// </summary>
    public string RedisConnectionString { get; set; } = string.Empty;
}
