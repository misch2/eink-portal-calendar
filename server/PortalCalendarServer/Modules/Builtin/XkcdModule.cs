using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for XKCD comic integration.
/// No config tab — the integration requires no user-configurable settings.
/// </summary>
public class XkcdModule : IPortalModule
{
    public string ModuleId => "xkcd";
    public string? ConfigTabDisplayName => null;
    public string? ConfigPartialView => null;

    public IReadOnlyList<string> OwnedConfigKeys => [];
    public IReadOnlyList<string> CheckboxConfigKeys => [];

    public object? CreateComponent(IServiceProvider services, Display display, DateTime date)
    {
        var context = services.GetRequiredService<CalendarContext>();
        var logger = services.GetRequiredService<ILogger<PageGeneratorService>>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var databaseCacheFactory = services.GetRequiredService<IDatabaseCacheServiceFactory>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        return new XkcdComponent(logger, httpClientFactory, memoryCache, databaseCacheFactory, context, loggerFactory);
    }
}
