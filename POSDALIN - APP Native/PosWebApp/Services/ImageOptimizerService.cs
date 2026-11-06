using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace PosWebApp.Services;

/// <summary>
/// Implements <see cref="IImageOptimizerService"/> using SixLabors.ImageSharp for
/// image resizing and encoding.
///
/// Processing pipeline:
///   1. Check <see cref="IMemoryCache"/> for a cached <see cref="OptimizedImage"/>.
///   2. On miss: acquire <see cref="SemaphoreSlim"/> (max 5 concurrent), then fetch the
///      original from the backend <c>hdImage_all.ashx</c> via <see cref="IServiceClient"/>.
///   3. Resize to requested dimensions (crop-to-fill, preserving aspect ratio).
///   4. Encode as WebP (primary) or JPEG (fallback).
///   5. Store result in cache with a 24-hour absolute expiration.
///
/// Response headers set by the /Images route handler: <c>Cache-Control: public, max-age=604800</c>
/// and <c>ETag</c> (SHA-256 of the encoded bytes).
///
/// **Validates: Requirements 12.1–12.11, 3.8, 3.9, 4.9, 5.7, 5.8, 16.4, 16.6**
/// </summary>
public sealed class ImageOptimizerService : IImageOptimizerService, IDisposable
{
    // ── Constants ────────────────────────────────────────────────────────────

    /// <summary>Max simultaneous image-processing operations (Requirement 16.6).</summary>
    private const int MaxConcurrency = 5;

    /// <summary>Server-side cache lifetime for optimized images (Requirement 16.4, 12.1).</summary>
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

    /// <summary>Browser / CDN cache lifetime in seconds (7 days) — Requirement 12.9.</summary>
    private const int BrowserMaxAgeSecs = 604800;

    // ── Fields ───────────────────────────────────────────────────────────────

    private readonly IServiceClient                     _serviceClient;
    private readonly IMemoryCache                       _cache;
    private readonly ILogger<ImageOptimizerService>     _logger;
    private readonly SemaphoreSlim                      _throttle = new(MaxConcurrency, MaxConcurrency);

    // ── Constructor ──────────────────────────────────────────────────────────

    public ImageOptimizerService(
        IServiceClient              serviceClient,
        IMemoryCache                cache,
        ILogger<ImageOptimizerService> logger)
    {
        _serviceClient = serviceClient;
        _cache         = cache;
        _logger        = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  IImageOptimizerService
    // ════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<OptimizedImage> GetOptimizedAsync(
        string      imageUrl,
        int         width,
        int         height,
        ImageFormat format = ImageFormat.WebP)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("imageUrl must not be empty.", nameof(imageUrl));
        if (width  <= 0) throw new ArgumentOutOfRangeException(nameof(width),  "Width must be positive.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");

        var cacheKey = BuildCacheKey(imageUrl, width, height, format);

        // ── 1. Cache hit ─────────────────────────────────────────────────
        if (_cache.TryGetValue(cacheKey, out OptimizedImage? cached) && cached is not null)
        {
            _logger.LogDebug("Image cache hit: {CacheKey}", cacheKey);
            return cached;
        }

        // ── 2. Throttle concurrent processing ────────────────────────────
        _logger.LogDebug("Image cache miss: {CacheKey} — waiting for semaphore", cacheKey);
        await _throttle.WaitAsync();
        try
        {
            // Re-check after acquiring semaphore (double-checked locking pattern).
            if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
            {
                _logger.LogDebug("Image cache hit (after semaphore): {CacheKey}", cacheKey);
                return cached;
            }

            // ── 3. Fetch original image ───────────────────────────────────
            _logger.LogInformation(
                "Fetching and optimizing image: {ImageUrl} at {Width}x{Height} ({Format})",
                imageUrl, width, height, format);

            await using var originalStream = await _serviceClient.GetImageStreamAsync(imageUrl);

            // ── 4. Resize + encode ────────────────────────────────────────
            var (data, contentType) = await ResizeAndEncodeAsync(originalStream, width, height, format);

            // ── 5. Compute ETag (SHA-256) ─────────────────────────────────
            var etag = ComputeETag(data);

            var optimized = new OptimizedImage(
                Data:        data,
                ContentType: contentType,
                ETag:        etag,
                Expires:     DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(BrowserMaxAgeSecs)));

            // ── 6. Store in cache with 24-hour absolute expiration ────────
            _cache.Set(cacheKey, optimized, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiry,
                // Estimate memory size as number of bytes in the image data.
                Size = data.Length
            });

            _logger.LogInformation(
                "Cached optimized image: {CacheKey} ({Bytes} bytes, ETag={ETag})",
                cacheKey, data.Length, etag);

            return optimized;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to optimize image: {ImageUrl} at {Width}x{Height}", imageUrl, width, height);
            throw;
        }
        finally
        {
            _throttle.Release();
        }
    }

    /// <inheritdoc />
    public string BuildSrcSet(string imageUrl, IEnumerable<(int width, int height)> sizes)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return string.Empty;

        var encoded = Uri.EscapeDataString(imageUrl);
        var entries = sizes
            .Where(s => s.width > 0 && s.height > 0)
            .Select(s => $"/Images/{encoded}?w={s.width}&h={s.height} {s.width}w");

        return string.Join(", ", entries);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Private helpers
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resizes the image from <paramref name="source"/> to fit within
    /// <paramref name="width"/> × <paramref name="height"/> using crop-to-fill,
    /// then encodes it in the requested format.
    /// </summary>
    private static async Task<(byte[] data, string contentType)> ResizeAndEncodeAsync(
        Stream      source,
        int         width,
        int         height,
        ImageFormat format)
    {
        using var image = await Image.LoadAsync(source);

        // Resize: crop-to-fill so the output is exactly width × height.
        image.Mutate(ctx =>
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Crop
            }));

        using var ms = new MemoryStream();

        if (format == ImageFormat.WebP)
        {
            await image.SaveAsWebpAsync(ms, new WebpEncoder
            {
                Quality = 80,    // good balance between quality and file size
                Method  = WebpEncodingMethod.Default
            });
            return (ms.ToArray(), "image/webp");
        }
        else
        {
            await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 82 });
            return (ms.ToArray(), "image/jpeg");
        }
    }

    /// <summary>
    /// Builds the <see cref="IMemoryCache"/> key from all dimensions that affect the output.
    /// SHA-256 hash of the URL prevents excessively long keys.
    /// </summary>
    private static string BuildCacheKey(string imageUrl, int width, int height, ImageFormat format)
    {
        var urlHash = ComputeUrlHash(imageUrl);
        var fmt     = format == ImageFormat.WebP ? "webp" : "jpeg";
        return $"img:{urlHash}:{width}x{height}:{fmt}";
    }

    /// <summary>Computes a short SHA-256 hex string for the URL to use in cache keys.</summary>
    private static string ComputeUrlHash(string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        // 16 hex chars (8 bytes) is enough to prevent collisions in a cache key.
        return Convert.ToHexString(bytes)[..16];
    }

    /// <summary>Computes a full SHA-256 hex ETag from the image payload (Requirement 12.11).</summary>
    private static string ComputeETag(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return $"\"{Convert.ToHexString(hash).ToLowerInvariant()}\"";
    }

    // ════════════════════════════════════════════════════════════════════════
    //  IDisposable
    // ════════════════════════════════════════════════════════════════════════

    public void Dispose() => _throttle.Dispose();
}
