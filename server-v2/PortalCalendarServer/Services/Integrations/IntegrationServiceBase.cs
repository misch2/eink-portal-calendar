using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Base class for integration services that fetch data from external APIs.
/// Provides both HTTP-level caching (via HttpClient with caching) and
/// database-level caching (via DatabaseCacheService).
/// Equivalent to PortalCalendar::Integration in Perl.
/// </summary>
public abstract class IntegrationServiceBase
{
    protected readonly ILogger Logger;
    protected readonly IMemoryCache MemoryCache;
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly IDatabaseCacheServiceFactory DatabaseCacheFactory;
    protected readonly CalendarContext Context;
    protected readonly Display? Display;
    private readonly int _minimalCacheExpiry;

    protected HttpClient httpClient => HttpClientFactory.CreateClient();

    protected IntegrationServiceBase(
        ILogger logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        IDatabaseCacheServiceFactory databaseCacheFactory,
        CalendarContext context,
        Display? display = null,
        int minimalCacheExpiry = 0)
    {
        HttpClientFactory = httpClientFactory;
        Logger = logger;
        MemoryCache = memoryCache;
        DatabaseCacheFactory = databaseCacheFactory;
        Context = context;
        Display = display;
        _minimalCacheExpiry = minimalCacheExpiry;
    }

    /// <summary>
    /// Maximum HTTP cache age in seconds. This is a short-term cache to prevent
    /// contacting the server too often. Default is 10 minutes.
    /// </summary>
    protected virtual int HttpMaxCacheAge => 10 * 60; // 10 minutes

    /// <summary>
    /// Get or create a DatabaseCacheService instance for this integration.
    /// The creator name is based on the class name.
    /// </summary>
    protected DatabaseCacheService GetDatabaseCache()
    {
        return DatabaseCacheFactory.Create(GetType().FullName ?? GetType().Name, _minimalCacheExpiry);
    }

    /// <summary>
    /// Clear the database cache for this integration
    /// </summary>
    public async Task ClearDatabaseCacheAsync(CancellationToken cancellationToken = default)
    {
        var dbCache = GetDatabaseCache();
        await dbCache.ClearAsync(cancellationToken);
    }
}
