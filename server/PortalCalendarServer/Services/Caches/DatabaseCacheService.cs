using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PortalCalendarServer.Services.Caches;

/// <summary>
/// Database-backed cache service for long-term caching of integration data.
/// Equivalent to PortalCalendar::DatabaseCache in Perl.
/// </summary>
public class DatabaseCacheService
{
    private readonly CalendarContext _context;
    private readonly ILogger<DatabaseCacheService> _logger;
    private readonly string _creator;
    private readonly TimeSpan _expiration;


    public DatabaseCacheService(
        CalendarContext context,
        ILogger<DatabaseCacheService> logger,
        string creator,
        TimeSpan expiration)
    {
        _context = context;
        _logger = logger;
        _creator = creator;
        _expiration = expiration;
    }

    /// <summary>
    /// Get or set a value in the cache. If the cache entry exists and is not expired,
    /// returns the cached value. Otherwise, executes the callback to get fresh data
    /// and stores it in the cache.
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(
        Func<Task<T>> callback,
        object cacheKeyParameters,
        CancellationToken cancellationToken = default)
    {
        var cacheKeyAsString = JsonSerializer.Serialize(cacheKeyParameters);
        var cacheKeyAsDigest = ComputeSha1Hash(cacheKeyAsString);

        var logPrefix = $"[{_creator}][key_json={cacheKeyAsString}] ";
        var now = DateTime.UtcNow;

        // Try to get from cache
        var row = await _context.Caches
            .Where(c => c.Creator == _creator
                     && c.Key == cacheKeyAsDigest
                     && c.ExpiresAt > now)
            .FirstOrDefaultAsync(cancellationToken);

        if (row != null && row.Data != null)
        {
            try
            {
                var data = JsonSerializer.Deserialize<T>(row.Data);
                var expiresInSeconds = (row.ExpiresAt - now).TotalSeconds;
                _logger.LogDebug(
                    "{LogPrefix}returning parsed data from cache (expires in {Seconds} seconds, at {ExpiresAt})",
                    logPrefix, expiresInSeconds, row.ExpiresAt);
                return data!;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "{LogPrefix}Failed to deserialize cached data, will recalculate", logPrefix);
            }
        }

        // Cache miss or expired - calculate fresh data
        _logger.LogInformation("{LogPrefix}recalculating fresh data", logPrefix);
        var freshData = await callback();

        // Store in cache
        _logger.LogDebug("{LogPrefix}storing serialized data into the DB", logPrefix);
        var serializedData = JsonSerializer.SerializeToUtf8Bytes(freshData);
        var expiresAt = now.Add(_expiration);

        var record = await _context.Caches
            .Where(c => c.Creator == _creator && c.Key == cacheKeyAsDigest)
            .FirstOrDefaultAsync(cancellationToken);

        if (record != null)
        {
            record.Data = serializedData;
            record.CreatedAt = now;
            record.ExpiresAt = expiresAt;
            _context.Update(record);
        }
        else
        {
            record = new Cache
            {
                Creator = _creator,
                Key = cacheKeyAsDigest,
                Data = serializedData,
                CreatedAt = now,
                ExpiresAt = expiresAt
            };
            _context.Caches.Add(record);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return freshData;
    }

    /// <summary>
    /// Clear all cache entries for this creator
    /// </summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing cached data for {Creator}", _creator);
        await _context.Caches
            .Where(c => c.Creator == _creator)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static string ComputeSha1Hash(string input)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
