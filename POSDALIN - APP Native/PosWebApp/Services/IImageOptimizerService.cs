namespace PosWebApp.Services;

/// <summary>
/// Supported output image formats for the Image_Optimizer.
/// </summary>
public enum ImageFormat
{
    WebP,
    Jpeg
}

/// <summary>
/// Represents an image that has been resized, encoded, and is ready to serve.
/// </summary>
/// <param name="Data">The encoded image bytes.</param>
/// <param name="ContentType">MIME type (e.g. <c>image/webp</c> or <c>image/jpeg</c>).</param>
/// <param name="ETag">SHA-256 hex digest of <paramref name="Data"/> for cache validation.</param>
/// <param name="Expires">Absolute expiry time used for the <c>Expires</c> response header.</param>
public record OptimizedImage(
    byte[]          Data,
    string          ContentType,
    string          ETag,
    DateTimeOffset  Expires);

/// <summary>
/// Resizes, compresses, and caches product images served by the backend
/// <c>hdImage_all.ashx</c> endpoint.
///
/// **Validates: Requirements 12.1–12.11, 3.8, 3.9, 4.9, 5.7, 5.8, 16.4, 16.6**
/// </summary>
public interface IImageOptimizerService
{
    /// <summary>
    /// Returns an optimized image for the given URL.
    /// The result is served from <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
    /// when available; otherwise the original is fetched, resized, encoded, and cached.
    /// </summary>
    /// <param name="imageUrl">Relative or absolute URL as passed to <c>hdImage_all.ashx</c>.</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <param name="format">Preferred output format; defaults to WebP.</param>
    Task<OptimizedImage> GetOptimizedAsync(
        string      imageUrl,
        int         width,
        int         height,
        ImageFormat format = ImageFormat.WebP);

    /// <summary>
    /// Builds an HTML <c>srcset</c> string with entries for each requested size.
    /// </summary>
    /// <param name="imageUrl">Original image URL (relative to the backend handler).</param>
    /// <param name="sizes">Sequence of <c>(width, height)</c> pairs to include in the srcset.</param>
    /// <returns>A comma-separated srcset value, e.g.
    /// <c>/Images/…?w=300&amp;h=200 300w, /Images/…?w=600&amp;h=400 600w</c>.</returns>
    string BuildSrcSet(string imageUrl, IEnumerable<(int width, int height)> sizes);
}
