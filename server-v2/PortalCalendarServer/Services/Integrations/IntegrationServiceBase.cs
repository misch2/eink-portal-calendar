using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Services.Integrations;

public abstract class IntegrationServiceBase : IIntegrationService
{
    protected readonly ILogger logger;
    protected readonly IMemoryCache memoryCache;
    protected readonly IHttpClientFactory httpClientFactory;
    protected readonly IDatabaseCacheServiceFactory databaseCacheFactory;
    protected readonly CalendarContext context;
    protected readonly Display? display;

    protected HttpClient httpClient => httpClientFactory.CreateClient();

    protected IntegrationServiceBase(
        ILogger logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        IDatabaseCacheServiceFactory databaseCacheFactory,
        CalendarContext context,
        Display? display = null
        )
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
        this.memoryCache = memoryCache;
        this.databaseCacheFactory = databaseCacheFactory;
        this.context = context;
        this.display = display;
    }

    public abstract bool IsConfigured();
}
