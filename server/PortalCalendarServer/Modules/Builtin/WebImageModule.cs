using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for displaying any web image.
/// 
/// </summary>
public class WebImageModule : IPortalModule
{
    public string ModuleId => "webimage";
    public string? ConfigTabDisplayName => null;
    public string? ConfigPartialView => null;

    public IReadOnlyList<string> OwnedConfigKeys => [
        "webimage_url", "webimage_cache_hours"
    ];
    public IReadOnlyList<string> CheckboxConfigKeys => [];

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date)
    {
        var context = services.GetRequiredService<CalendarContext>();
        var displayService = services.GetRequiredService<IDisplayService>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var databaseCacheFactory = services.GetRequiredService<IDatabaseCacheServiceFactory>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        return new WebImageComponent(displayService, httpClientFactory, memoryCache, databaseCacheFactory, context, loggerFactory);
    }
}
