using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;

namespace PortalCalendarServer.Services.Caches;

/// <summary>
/// Utility service for managing all caches in the application.
/// Equivalent to PortalCalendar::Command::nuke_caches in Perl.
/// </summary>
public class CacheManagementService
{
    private readonly CalendarContext _context;
    private readonly ILogger<CacheManagementService> _logger;

    public CacheManagementService(
        CalendarContext context,
        ILogger<CacheManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Clear all database cache entries
    /// </summary>
    public async Task ClearAllDatabaseCachesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing all database caches");
        var deletedCount = await _context.Caches.ExecuteDeleteAsync(cancellationToken);
        _logger.LogInformation("Deleted {Count} cache entries", deletedCount);
    }

    /// <summary>
    /// Clear database cache entries for a specific creator (integration service)
    /// </summary>
    public async Task ClearDatabaseCacheByCreatorAsync(
        string creator,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing database cache for creator: {Creator}", creator);
        var deletedCount = await _context.Caches
            .Where(c => c.Creator == creator)
            .ExecuteDeleteAsync(cancellationToken);
        _logger.LogInformation("Deleted {Count} cache entries for {Creator}", deletedCount, creator);
    }

    /// <summary>
    /// Clear expired cache entries
    /// </summary>
    public async Task ClearExpiredCachesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing expired database caches");
        var now = DateTime.UtcNow;
        var deletedCount = await _context.Caches
            .Where(c => c.ExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);
        _logger.LogInformation("Deleted {Count} expired cache entries", deletedCount);
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public async Task<CacheStatistics> GetCacheStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var stats = new CacheStatistics
        {
            TotalEntries = await _context.Caches.CountAsync(cancellationToken),
            ExpiredEntries = await _context.Caches
                .Where(c => c.ExpiresAt < now)
                .CountAsync(cancellationToken),
            ActiveEntries = await _context.Caches
                .Where(c => c.ExpiresAt >= now)
                .CountAsync(cancellationToken),
            TotalSizeBytes = await _context.Caches
                .Where(c => c.Data != null)
                .SumAsync(c => c.Data!.Length, cancellationToken),
            CreatorStats = await _context.Caches
                .GroupBy(c => c.Creator)
                .Select(g => new CreatorCacheStats
                {
                    Creator = g.Key,
                    Count = g.Count(),
                    SizeBytes = g.Where(c => c.Data != null).Sum(c => c.Data!.Length)
                })
                .OrderByDescending(s => s.Count)
                .ToListAsync(cancellationToken)
        };

        return stats;
    }

    /// <summary>
    /// Get detailed information about cache entries
    /// </summary>
    public async Task<List<CacheEntryInfo>> GetCacheEntriesAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.Caches
            .OrderByDescending(c => c.ExpiresAt)
            .ThenByDescending(c => c.Id)
            .Take(limit)
            .Select(c => new CacheEntryInfo
            {
                Id = c.Id,
                Creator = c.Creator,
                Key = c.Key,
                CreatedAt = c.CreatedAt,
                ExpiresAt = c.ExpiresAt,
                SizeBytes = c.Data != null ? c.Data.Length : 0,
                IsExpired = c.ExpiresAt < DateTime.UtcNow
            })
            .ToListAsync(cancellationToken);
    }
}

public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public int ActiveEntries { get; set; }
    public long TotalSizeBytes { get; set; }
    public List<CreatorCacheStats> CreatorStats { get; set; } = new();
}

public class CreatorCacheStats
{
    public string Creator { get; set; } = string.Empty;
    public int Count { get; set; }
    public long SizeBytes { get; set; }
}

public class CacheEntryInfo
{
    public int Id { get; set; }
    public string Creator { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int SizeBytes { get; set; }
    public bool IsExpired { get; set; }
}
