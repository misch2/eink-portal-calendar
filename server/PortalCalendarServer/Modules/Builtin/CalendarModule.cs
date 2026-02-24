using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for ICS calendar integration.
/// Provides the Calendar config tab and the <see cref="CalendarComponent"/>.
/// </summary>
public class CalendarModule : IPortalModule
{
    public string ModuleId => "calendar";
    public string? ConfigTabDisplayName => "Calendar (ICS)";
    public string? ConfigPartialView => "ConfigUI/_Calendar";

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "web_calendar_ics_url1", "web_calendar_ics_url2", "web_calendar_ics_url3",
        "web_calendar1", "web_calendar2", "web_calendar3"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys =>
    [
        "web_calendar1", "web_calendar2", "web_calendar3"
    ];

    public object? CreateComponent(IServiceProvider services, Display display, DateTime date)
    {
        var context = services.GetRequiredService<CalendarContext>();
        var logger = services.GetRequiredService<ILogger<PageGeneratorService>>();
        var displayService = services.GetRequiredService<IDisplayService>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var databaseCacheFactory = services.GetRequiredService<IDatabaseCacheServiceFactory>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        return new CalendarComponent(logger, displayService, httpClientFactory, memoryCache, databaseCacheFactory, context, loggerFactory);
    }
}
