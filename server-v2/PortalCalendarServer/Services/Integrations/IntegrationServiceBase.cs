using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models;
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
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly IMemoryCache MemoryCache;
    protected readonly CalendarContext Context;
    protected readonly Display? Display;
    private readonly int _minimalCacheExpiry;

    protected IntegrationServiceBase(
        ILogger logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        CalendarContext context,
        Display? display = null,
        int minimalCacheExpiry = 0)
    {
        Logger = logger;
        HttpClientFactory = httpClientFactory;
        MemoryCache = memoryCache;
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
        // Create a logger factory for the DatabaseCacheService
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
        });
        
        return new DatabaseCacheService(
            Context,
            loggerFactory.CreateLogger<DatabaseCacheService>(),
            GetType().FullName ?? GetType().Name,
            _minimalCacheExpiry);
    }

    /// <summary>
    /// Get an HttpClient instance configured for this integration.
    /// The client is configured with appropriate timeouts and headers.
    /// </summary>
    protected HttpClient GetHttpClient()
    {
        var client = HttpClientFactory.CreateClient(GetType().Name);
        
        // Set a default User-Agent if not already set
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            client.DefaultRequestHeaders.Add("User-Agent", 
                "PortalCalendar/2.0 github.com/misch2/eink-portal-calendar");
        }

        return client;
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
