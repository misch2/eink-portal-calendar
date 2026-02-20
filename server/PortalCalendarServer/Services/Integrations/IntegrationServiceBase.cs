using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Services.Integrations;

public abstract class IntegrationServiceBase : IIntegrationService
{
    protected readonly ILogger logger;
    protected readonly IMemoryCache memoryCache;
    protected readonly IHttpClientFactory httpClientFactory;
    protected readonly IDatabaseCacheServiceFactory databaseCacheFactory;
    protected readonly CalendarContext context;

    protected HttpClient httpClient => httpClientFactory.CreateClient();

    protected IntegrationServiceBase(
        ILogger logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        IDatabaseCacheServiceFactory databaseCacheFactory,
        CalendarContext context
        )
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
        this.memoryCache = memoryCache;
        this.databaseCacheFactory = databaseCacheFactory;
        this.context = context;
    }

    public abstract bool IsConfigured(Display display);
}
