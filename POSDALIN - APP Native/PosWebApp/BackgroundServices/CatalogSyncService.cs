using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using PosWebApp.Services;

namespace PosWebApp.BackgroundServices;

/// <summary>
/// Hosted background service that periodically invalidates the product-catalog
/// cache and re-primes it by calling
/// <see cref="IProductCatalogService.GetCategoriesAsync"/>.
///
/// Key behaviours (Requirements 21.1 – 21.10):
/// <list type="bullet">
///   <item>Runs on a configurable interval (default 15 minutes — <c>Sync:IntervalMinutes</c>).</item>
///   <item>Acquires a distributed lock (<c>sync:lock</c>) via <see cref="IDistributedCache"/>
///         to prevent concurrent sync in multi-instance deployments.</item>
///   <item>Logs sync start, completion, and any errors.</item>
///   <item>Retries up to 3 times with exponential back-off (1 s → 2 s → 4 s) on failure.</item>
///   <item>Stores the last successful sync timestamp in <see cref="IMemoryCache"/>
///         under key <c>"sync:lastTimestamp"</c> — no database write.</item>
///   <item>Continues serving cached data on sync failure (cache is not cleared on error).</item>
/// </list>
///
/// **Validates: Requirements 21.1, 21.2, 21.3, 21.4, 21.5, 21.6, 21.7, 21.8, 21.9, 21.10**
/// </summary>
public sealed class CatalogSyncService : BackgroundService
{
    // ── Cache keys ──────────────────────────────────────────────────────────

    /// <summary>Distributed-cache key used as a mutex for the sync operation.</summary>
    internal const string LockKey = "sync:lock";

    /// <summary>Memory-cache key where the last successful sync timestamp is stored.</summary>
    internal const string LastTimestampKey = "sync:lastTimestamp";

    /// <summary>How long the distributed lock is held before it auto-expires.</summary>
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(5);

    // ── Exponential back-off delays (1 s, 2 s, 4 s) ────────────────────────

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
    ];

    // ── Dependencies ────────────────────────────────────────────────────────

    private readonly IProductCatalogService _catalogService;
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CatalogSyncService> _logger;
    private readonly TimeSpan _syncInterval;

    // ── Constructor ─────────────────────────────────────────────────────────

    public CatalogSyncService(
        IProductCatalogService catalogService,
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        IConfiguration configuration,
        ILogger<CatalogSyncService> logger)
    {
        _catalogService  = catalogService;
        _distributedCache = distributedCache;
        _memoryCache      = memoryCache;
        _logger           = logger;

        var intervalMinutes = configuration.GetValue<int>("Sync:IntervalMinutes", 15);
        _syncInterval = TimeSpan.FromMinutes(intervalMinutes > 0 ? intervalMinutes : 15);

        _logger.LogInformation(
            "CatalogSyncService configured with sync interval of {IntervalMinutes} minutes.",
            _syncInterval.TotalMinutes);
    }

    // ── BackgroundService implementation ────────────────────────────────────

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CatalogSyncService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutting down — exit cleanly.
                break;
            }

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunSyncCycleAsync(stoppingToken);
        }

        _logger.LogInformation("CatalogSyncService stopped.");
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to acquire the distributed lock and, if successful, runs the
    /// full invalidate → re-prime cycle with exponential back-off on failure.
    /// </summary>
    private async Task RunSyncCycleAsync(CancellationToken ct)
    {
        // ── Acquire distributed lock (NX semantics) ────────────────────────
        if (!await TryAcquireLockAsync(ct))
        {
            _logger.LogInformation(
                "CatalogSyncService skipping sync cycle — lock '{LockKey}' is held by another instance.",
                LockKey);
            return;
        }

        _logger.LogInformation("CatalogSyncService sync cycle started.");

        try
        {
            await ExecuteSyncWithRetryAsync(ct);
        }
        finally
        {
            await ReleaseLockAsync();
        }
    }

    /// <summary>
    /// Executes the invalidate → re-prime sync, retrying up to 3 times with
    /// exponential back-off.  On permanent failure the cache is left intact so
    /// the application continues serving stale-but-valid data.
    /// </summary>
    private async Task ExecuteSyncWithRetryAsync(CancellationToken ct)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= RetryDelays.Length; attempt++)
        {
            if (ct.IsCancellationRequested)
                return;

            try
            {
                // 1. Invalidate all catalog cache entries.
                await _catalogService.InvalidateCatalogCacheAsync();

                // 2. Re-prime the categories cache (warms the most-used catalog path).
                await _catalogService.GetCategoriesAsync();

                // 3. Record the successful sync timestamp (in-memory, no DB write).
                var now = DateTimeOffset.UtcNow;
                _memoryCache.Set(LastTimestampKey, now);

                _logger.LogInformation(
                    "CatalogSyncService sync cycle completed successfully at {Timestamp}.",
                    now);

                return; // Success — exit retry loop.
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("CatalogSyncService sync cancelled during attempt {Attempt}.", attempt + 1);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(ex,
                    "CatalogSyncService sync attempt {Attempt}/{MaxAttempts} failed.",
                    attempt + 1, RetryDelays.Length + 1);

                if (attempt < RetryDelays.Length)
                {
                    var delay = RetryDelays[attempt];
                    _logger.LogInformation(
                        "CatalogSyncService retrying in {DelaySeconds} seconds (attempt {Next}/{Max}).",
                        delay.TotalSeconds, attempt + 2, RetryDelays.Length + 1);

                    try
                    {
                        await Task.Delay(delay, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        // All retries exhausted — log final failure but do NOT clear the cache
        // so that callers continue serving previously-cached data.
        _logger.LogError(lastException,
            "CatalogSyncService sync failed after {MaxAttempts} attempts. Cached data will continue to be served.",
            RetryDelays.Length + 1);
    }

    /// <summary>
    /// Tries to set the distributed lock key with a 5-minute absolute expiry.
    /// Returns <see langword="true"/> if the lock was acquired (key was absent),
    /// <see langword="false"/> if another instance already holds the lock.
    /// </summary>
    private async Task<bool> TryAcquireLockAsync(CancellationToken ct)
    {
        try
        {
            // Check whether the lock key already exists (NX — only set if absent).
            var existing = await _distributedCache.GetAsync(LockKey, ct);
            if (existing is not null)
                return false;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = LockTtl
            };

            // Store the current timestamp as the lock value.
            var lockValue = System.Text.Encoding.UTF8.GetBytes(
                DateTimeOffset.UtcNow.ToString("O"));

            await _distributedCache.SetAsync(LockKey, lockValue, options, ct);

            // Re-read to confirm we own the lock (handles rare race between
            // two instances that both saw null at step 1).
            var written = await _distributedCache.GetAsync(LockKey, ct);
            if (written is null)
                return false;

            var writtenValue = System.Text.Encoding.UTF8.GetString(written);
            var ourValue     = System.Text.Encoding.UTF8.GetString(lockValue);
            return string.Equals(writtenValue, ourValue, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            // If the distributed cache is unavailable we skip the sync rather
            // than crashing the entire hosted service.
            _logger.LogWarning(ex,
                "CatalogSyncService could not reach the distributed cache to acquire the sync lock. Skipping cycle.");
            return false;
        }
    }

    /// <summary>
    /// Releases the distributed lock by deleting the lock key.
    /// Errors are swallowed so that the lock TTL acts as the safety net.
    /// </summary>
    private async Task ReleaseLockAsync()
    {
        try
        {
            await _distributedCache.RemoveAsync(LockKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "CatalogSyncService could not release the distributed lock '{LockKey}'. It will expire automatically.",
                LockKey);
        }
    }
}
