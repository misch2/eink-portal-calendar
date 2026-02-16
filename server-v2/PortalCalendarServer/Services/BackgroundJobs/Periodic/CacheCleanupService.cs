using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Services.BackgroundJobs.Periodic;

/// <summary>
/// Background service that periodically cleans expired cache entries.
/// Runs every hour by default.
/// </summary>
public class CacheCleanupService : PeriodicBackgroundService
{
    private readonly ILogger<CacheCleanupService> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _startupDelay;

    public CacheCleanupService(
        ILogger<CacheCleanupService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration) : base(logger, serviceScopeFactory)
    {
        _logger = logger;

        // Read cleanup interval from configuration, default to 1 hour
        var intervalMinutes = configuration.GetValue<int>("BackgroundJobs:CacheCleanup:IntervalMinutes");
        _interval = TimeSpan.FromMinutes(intervalMinutes);

        // Startup delay to avoid running immediately on application start, default to 5 minutes
        intervalMinutes = configuration.GetValue<int>("BackgroundJobs:CacheCleanup:StartupDelayMinutes");
        _startupDelay = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override TimeSpan Interval => _interval;

    protected override TimeSpan StartupDelay => _startupDelay;

    protected override string ServiceName => "Cache Cleanup Service";

    protected override async Task ExecuteWorkAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking for expired cache entries to clean up");

        await using var scope = ServiceScopeFactory.CreateAsyncScope();
        var displayService = scope.ServiceProvider.GetRequiredService<IDisplayService>();
        var cacheManagementService = scope.ServiceProvider.GetRequiredService<CacheManagementService>();

        var stats = await cacheManagementService.GetCacheStatisticsAsync(cancellationToken);
        _logger.LogInformation(
            "Current cache stats: {Total} total entries, {Expired} expired, {Active} active, {SizeMB:F2} MB",
            stats.TotalEntries,
            stats.ExpiredEntries,
            stats.ActiveEntries,
            stats.TotalSizeBytes / 1024.0 / 1024.0);

        if (stats.ExpiredEntries > 0)
        {
            await cacheManagementService.ClearExpiredCachesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} expired cache entries", stats.ExpiredEntries);
        }
        else
        {
            _logger.LogDebug("No expired cache entries to clean up");
        }
    }
}
