using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Services.Integrations;

public abstract class IntegrationServiceBase : IIntegrationService
{
    protected readonly ILogger Logger;
    protected readonly IMemoryCache MemoryCache;
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly IDatabaseCacheServiceFactory DatabaseCacheFactory;
    protected readonly CalendarContext Context;
    protected readonly Display? Display;

    protected HttpClient httpClient => HttpClientFactory.CreateClient();

    protected IntegrationServiceBase(
        ILogger logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        IDatabaseCacheServiceFactory databaseCacheFactory,
        CalendarContext context,
        Display? display = null
        )
    {
        HttpClientFactory = httpClientFactory;
        Logger = logger;
        MemoryCache = memoryCache;
        DatabaseCacheFactory = databaseCacheFactory;
        Context = context;
        Display = display;
    }

    public abstract bool IsConfigured();
}
