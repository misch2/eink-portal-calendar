namespace PortalCalendarServer.Services.Caches;

/// <summary>
/// Background service that periodically cleans expired cache entries.
/// Runs every hour by default.
/// </summary>
public class CacheCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheCleanupService> _logger;
    private readonly TimeSpan _interval;

    public CacheCleanupService(
        IServiceProvider serviceProvider,
        ILogger<CacheCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Read cleanup interval from configuration, default to 1 hour
        var intervalMinutes = configuration.GetValue<int?>("Cache:CleanupIntervalMinutes") ?? 60;
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Cache Cleanup Service started. Will run every {Minutes} minutes.",
            _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await CleanupExpiredCachesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
                // Continue running even if cleanup fails
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Cache Cleanup Service stopped.");
    }

    private async Task CleanupExpiredCachesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting cache cleanup...");

        try
        {
            // Create a scope to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var cacheManagement = scope.ServiceProvider.GetRequiredService<CacheManagementService>();

            var stats = await cacheManagement.GetCacheStatisticsAsync(cancellationToken);

            _logger.LogInformation(
                "Current cache stats: {Total} total entries, {Expired} expired, {Active} active, {SizeMB:F2} MB",
                stats.TotalEntries,
                stats.ExpiredEntries,
                stats.ActiveEntries,
                stats.TotalSizeBytes / 1024.0 / 1024.0);

            if (stats.ExpiredEntries > 0)
            {
                await cacheManagement.ClearExpiredCachesAsync(cancellationToken);
                _logger.LogInformation("Cleaned up {Count} expired cache entries", stats.ExpiredEntries);
            }
            else
            {
                _logger.LogDebug("No expired cache entries to clean up");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired caches");
            throw;
        }
    }
}
