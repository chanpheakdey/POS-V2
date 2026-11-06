using System.ComponentModel.DataAnnotations;

namespace PosWebApp.Infrastructure.Options;

/// <summary>
/// Strongly-typed binding for the <c>Database</c> section in appsettings.json.
/// Configures the ADO.NET connection to SQL Server for stored procedure calls.
/// Requirements: 24.1, 24.2, 24.3
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// SQL Server command timeout in seconds (default 30).
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts on transient SQL errors (default 3).
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Base URL for the image handler endpoint (e.g. hdImage_all.ashx).
    /// If empty, image fetching falls back to a local placeholder.
    /// </summary>
    public string ImageBaseUrl { get; set; } = string.Empty;
}