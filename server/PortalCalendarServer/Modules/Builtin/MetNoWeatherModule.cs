using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for Met.no weather integration.
/// Provides the Met.no config tab and the <see cref="WeatherComponent"/>.
/// </summary>
public class MetNoWeatherModule : IPortalModule
{
    public string ModuleId => "metnoweather";
    public string? ConfigTabDisplayName => "Met.no weather";
    public string? ConfigPartialView => "ConfigUI/_MetNoWeather";

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "metnoweather", "metnoweather_granularity_hours", "lat", "lon", "alt"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => ["metnoweather"];

    public object? CreateComponent(IServiceProvider services, Display display, DateTime date)
    {
        var context = services.GetRequiredService<CalendarContext>();
        var logger = services.GetRequiredService<ILogger<PageGeneratorService>>();
        var displayService = services.GetRequiredService<IDisplayService>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var databaseCacheFactory = services.GetRequiredService<IDatabaseCacheServiceFactory>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        return new WeatherComponent(logger, displayService, httpClientFactory, memoryCache, databaseCacheFactory, context, loggerFactory);
    }
}
