namespace PosWebApp.Infrastructure.Options;

/// <summary>
/// Strongly-typed binding for the <c>Image</c> section in appsettings.json.
/// Controls the target dimensions used by <see cref="PosWebApp.Services.ImageOptimizerService"/>
/// and the maximum parallel processing concurrency.
/// Requirements: 25.8, 25.9
/// </summary>
public sealed class ImageOptions
{
    public const string SectionName = "Image";

    /// <summary>Target dimensions for category images (default 300×200 px).</summary>
    public ImageSize CategorySize { get; set; } = new(300, 200);

    /// <summary>Target dimensions for item thumbnail images (default 200×110 px).</summary>
    public ImageSize ItemThumbnailSize { get; set; } = new(200, 110);

    /// <summary>
    /// Maximum number of images that may be processed concurrently.
    /// Default: 5.
    /// Override via environment variable: <c>Image__MaxConcurrentProcessing</c>.
    /// </summary>
    public int MaxConcurrentProcessing { get; set; } = 5;
}

/// <summary>
/// Width × height pair used by <see cref="ImageOptions"/>.
/// </summary>
public sealed class ImageSize
{
    /// <summary>Parameterless constructor required for configuration binding.</summary>
    public ImageSize() { }

    public ImageSize(int width, int height)
    {
        Width  = width;
        Height = height;
    }

    /// <summary>Target width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Target height in pixels.</summary>
    public int Height { get; set; }
}
