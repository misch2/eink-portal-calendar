using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Services.Integrations;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for Google Fit integration.
/// Provides the Google Fit config tab and the <see cref="WeightComponent"/>.
/// </summary>
public class GoogleFitModule : IPortalModule
{
    public string ModuleId => "googlefit";
    public string? ConfigTabDisplayName => "Google Fit";
    public string? ConfigPartialView => "ConfigUI/_GoogleFit";

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "googlefit", "googlefit_client_id", "googlefit_client_secret"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => ["googlefit"];

    public object? CreateComponent(IServiceProvider services, Display display, DateTime date)
    {
        var context = services.GetRequiredService<CalendarContext>();
        var logger = services.GetRequiredService<ILogger<PageGeneratorService>>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var databaseCacheFactory = services.GetRequiredService<IDatabaseCacheServiceFactory>();
        var displayService = services.GetRequiredService<IDisplayService>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        var googleFitService = new GoogleFitIntegrationService(
            loggerFactory.CreateLogger<GoogleFitIntegrationService>(),
            httpClientFactory,
            memoryCache,
            databaseCacheFactory,
            context,
            displayService);

        return new WeightComponent(logger, googleFitService, display);
    }
}
