using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PosWebApp.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PosWebApp.Tests.Integration;

/// <summary>
/// Unit tests for <see cref="ImageOptimizerService"/> covering:
/// semaphore throttling, deterministic ETag generation, WebP MIME type, and JPEG fallback.
///
/// **Validates: Requirements 12.6, 12.11, 16.6**
/// </summary>
public class ImageOptimizerTests : IDisposable
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a 10×10 white PNG in memory using ImageSharp and returns it as a MemoryStream.
    /// Using a fresh stream each time prevents position issues across multiple reads.
    /// </summary>
    private static MemoryStream CreateTestPng()
    {
        var ms = new MemoryStream();
        using var image = new Image<Rgba32>(10, 10);
        image.SaveAsPng(ms);
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Builds a <see cref="ImageOptimizerService"/> wired to the given service-client mock
    /// and a fresh <see cref="MemoryCache"/>.
    /// </summary>
    private static ImageOptimizerService CreateSut(Mock<IServiceClient> serviceClientMock)
    {
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50 * 1024 * 1024 });
        return new ImageOptimizerService(
            serviceClientMock.Object,
            cache,
            NullLogger<ImageOptimizerService>.Instance);
    }

    // ── Test fixtures ─────────────────────────────────────────────────────────

    private readonly Mock<IServiceClient> _serviceClientMock = new();

    public void Dispose() { }

    // ════════════════════════════════════════════════════════════════════════
    // 1. Semaphore throttling — Requirement 16.6
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fire 10 concurrent requests and verify no more than 5 are processed at the
    /// same time.  A barrier inside the mock tracks the observed concurrency level.
    ///
    /// **Validates: Requirement 16.6**
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_AtMost5RequestsRunConcurrently()
    {
        const int totalRequests = 10;
        const int maxExpectedConcurrency = 5;

        int currentlyRunning = 0;
        int maxObservedConcurrency = 0;
        var syncLock = new object();

        // Each simulated request increments the counter, pauses briefly to let
        // other concurrent tasks reach this point, then decrements.
        _serviceClientMock
            .Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
            .Returns(async () =>
            {
                int running;
                lock (syncLock)
                {
                    running = Interlocked.Increment(ref currentlyRunning);
                    if (running > maxObservedConcurrency)
                        maxObservedConcurrency = running;
                }

                // Small delay so concurrent tasks overlap in the "in-flight" window.
                await Task.Delay(50);

                Interlocked.Decrement(ref currentlyRunning);
                return (Stream)CreateTestPng();
            });

        using var sut = CreateSut(_serviceClientMock);

        // Each request uses a unique URL so none of them hit the cache.
        var tasks = Enumerable.Range(0, totalRequests)
            .Select(i => sut.GetOptimizedAsync(
                imageUrl: $"http://backend/img/{i}.png",
                width:    10,
                height:   10,
                format:   ImageFormat.WebP))
            .ToList();

        await Task.WhenAll(tasks);

        Assert.True(
            maxObservedConcurrency <= maxExpectedConcurrency,
            $"Expected at most {maxExpectedConcurrency} concurrent operations, " +
            $"but observed {maxObservedConcurrency}.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 2. ETag is deterministic for the same image content — Requirement 12.11
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// When the same image bytes are encoded twice, the resulting ETag (SHA-256)
    /// must be identical.  Two separate <see cref="ImageOptimizerService"/> instances
    /// (each with its own empty cache) are used to ensure the second call is not
    /// merely a cache hit.
    ///
    /// **Validates: Requirement 12.11**
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_SameContent_ProducesDeterministicETag()
    {
        // Use the same image bytes for both calls.
        var mock1 = new Mock<IServiceClient>();
        mock1.Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
             .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        var mock2 = new Mock<IServiceClient>();
        mock2.Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
             .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        using var sut1 = CreateSut(mock1);
        using var sut2 = CreateSut(mock2);

        const string imageUrl = "http://backend/img/deterministic.png";

        var result1 = await sut1.GetOptimizedAsync(imageUrl, 10, 10, ImageFormat.WebP);
        var result2 = await sut2.GetOptimizedAsync(imageUrl, 10, 10, ImageFormat.WebP);

        // ETags are quoted SHA-256 hex strings — they must be equal.
        Assert.False(string.IsNullOrWhiteSpace(result1.ETag), "ETag should not be empty.");
        Assert.Equal(result1.ETag, result2.ETag);
    }

    /// <summary>
    /// ETag format must be a quoted lowercase SHA-256 hex string (64 hex chars in double-quotes).
    ///
    /// **Validates: Requirement 12.11**
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_ETag_IsQuotedSha256HexString()
    {
        _serviceClientMock
            .Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        using var sut = CreateSut(_serviceClientMock);

        var result = await sut.GetOptimizedAsync("http://backend/img/etag-format.png", 10, 10);

        // Expected format: "\"<64 lowercase hex chars>\""
        Assert.Matches(@"^""[0-9a-f]{64}""$", result.ETag);
    }

    /// <summary>
    /// The ETag must equal the SHA-256 hash of the returned image bytes.
    /// This explicitly proves the ETag is computed from the payload (not a constant
    /// or a hash of the URL) — Requirement 12.11.
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_ETag_EqualsSha256OfReturnedData()
    {
        _serviceClientMock
            .Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        using var sut = CreateSut(_serviceClientMock);

        var result = await sut.GetOptimizedAsync("http://backend/img/sha256-verify.png", 10, 10);

        // Compute the expected ETag from the actual returned bytes: quoted lowercase SHA-256 hex.
        var expectedHash = SHA256.HashData(result.Data);
        var expectedETag = $"\"{Convert.ToHexString(expectedHash).ToLowerInvariant()}\"";

        Assert.Equal(expectedETag, result.ETag);
    }

    /// <summary>
    /// For the same URL, the second call (cache hit) returns the same ETag as the first.
    ///
    /// **Validates: Requirement 12.11**
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_CacheHit_ReturnsSameETag()
    {
        _serviceClientMock
            .Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        using var sut = CreateSut(_serviceClientMock);

        const string url = "http://backend/img/cache-etag.png";
        var first  = await sut.GetOptimizedAsync(url, 10, 10);
        var second = await sut.GetOptimizedAsync(url, 10, 10);

        Assert.Equal(first.ETag, second.ETag);
        // The second call should be a cache hit — backend called only once.
        _serviceClientMock.Verify(s => s.GetImageStreamAsync(url), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 3. Correct MIME type for WebP output — Requirement 12.6
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// When <see cref="ImageFormat.WebP"/> is requested (the default), the returned
    /// <see cref="OptimizedImage.ContentType"/> must be <c>image/webp</c>.
    ///
    /// **Validates: Requirement 12.6**
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_WebPFormat_ReturnsImageWebpContentType()
    {
        _serviceClientMock
            .Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        using var sut = CreateSut(_serviceClientMock);

        var result = await sut.GetOptimizedAsync(
            "http://backend/img/webp-mime.png", 10, 10, ImageFormat.WebP);

        Assert.Equal("image/webp", result.ContentType);
    }

    /// <summary>
    /// The default format (no explicit <see cref="ImageFormat"/> argument) also
    /// returns <c>image/webp</c>.
    ///
    /// **Validates: Requirement 12.6**
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_DefaultFormat_ReturnsImageWebpContentType()
    {
        _serviceClientMock
            .Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        using var sut = CreateSut(_serviceClientMock);

        // No explicit format → default is WebP
        var result = await sut.GetOptimizedAsync("http://backend/img/default-mime.png", 10, 10);

        Assert.Equal("image/webp", result.ContentType);
    }

    /// <summary>
    /// The response data must not be empty — the encoded WebP bytes are present.
    ///
    /// **Validates: Requirement 12.6**
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_WebPFormat_ReturnNonEmptyData()
    {
        _serviceClientMock
            .Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        using var sut = CreateSut(_serviceClientMock);

        var result = await sut.GetOptimizedAsync(
            "http://backend/img/nonempty.png", 10, 10, ImageFormat.WebP);

        Assert.NotNull(result.Data);
        Assert.True(result.Data.Length > 0, "Encoded image data should not be empty.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 4. JPEG fallback — Requirement 12.6
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// When <see cref="ImageFormat.Jpeg"/> is explicitly requested, the returned
    /// <see cref="OptimizedImage.ContentType"/> must be <c>image/jpeg</c>.
    ///
    /// **Validates: Requirement 12.6**
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_JpegFormat_ReturnsImageJpegContentType()
    {
        _serviceClientMock
            .Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        using var sut = CreateSut(_serviceClientMock);

        var result = await sut.GetOptimizedAsync(
            "http://backend/img/jpeg-fallback.png", 10, 10, ImageFormat.Jpeg);

        Assert.Equal("image/jpeg", result.ContentType);
    }

    /// <summary>
    /// JPEG encoding must produce non-empty bytes.
    ///
    /// **Validates: Requirement 12.6**
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_JpegFormat_ReturnsNonEmptyData()
    {
        _serviceClientMock
            .Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
            .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        using var sut = CreateSut(_serviceClientMock);

        var result = await sut.GetOptimizedAsync(
            "http://backend/img/jpeg-nonempty.png", 10, 10, ImageFormat.Jpeg);

        Assert.NotNull(result.Data);
        Assert.True(result.Data.Length > 0, "JPEG-encoded image data should not be empty.");
    }

    /// <summary>
    /// WebP and JPEG output for the same source must differ in content
    /// (the encoder really did produce different formats).
    ///
    /// **Validates: Requirement 12.6**
    /// </summary>
    [Fact]
    public async Task GetOptimizedAsync_WebPAndJpeg_ProduceDifferentOutputs()
    {
        var mockWebP = new Mock<IServiceClient>();
        mockWebP.Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
                .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        var mockJpeg = new Mock<IServiceClient>();
        mockJpeg.Setup(s => s.GetImageStreamAsync(It.IsAny<string>()))
                .Returns(() => Task.FromResult<Stream>(CreateTestPng()));

        using var sutWebP = CreateSut(mockWebP);
        using var sutJpeg = CreateSut(mockJpeg);

        var webpResult = await sutWebP.GetOptimizedAsync(
            "http://backend/img/format-compare.png", 10, 10, ImageFormat.WebP);
        var jpegResult = await sutJpeg.GetOptimizedAsync(
            "http://backend/img/format-compare.png", 10, 10, ImageFormat.Jpeg);

        // Content-type must differ.
        Assert.NotEqual(webpResult.ContentType, jpegResult.ContentType);

        // ETag (SHA-256 of payload) must differ because the encoded bytes differ.
        Assert.NotEqual(webpResult.ETag, jpegResult.ETag);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 5. Guard clauses
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetOptimizedAsync_EmptyImageUrl_ThrowsArgumentException(string? url)
    {
        using var sut = CreateSut(new Mock<IServiceClient>());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetOptimizedAsync(url!, 10, 10));
    }

    [Fact]
    public async Task GetOptimizedAsync_ZeroWidth_ThrowsArgumentOutOfRangeException()
    {
        using var sut = CreateSut(new Mock<IServiceClient>());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.GetOptimizedAsync("http://backend/img/x.png", 0, 10));
    }

    [Fact]
    public async Task GetOptimizedAsync_ZeroHeight_ThrowsArgumentOutOfRangeException()
    {
        using var sut = CreateSut(new Mock<IServiceClient>());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.GetOptimizedAsync("http://backend/img/x.png", 10, 0));
    }
}
