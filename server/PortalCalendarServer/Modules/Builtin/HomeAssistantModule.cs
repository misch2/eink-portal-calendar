using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for Home Assistant integration.
/// Provides config tab for HA connection settings and a <see cref="HomeAssistantComponent"/>
/// that can fetch entity states from the Home Assistant REST API.
/// </summary>
public class HomeAssistantModule : IPortalModule
{
    public string ModuleId => "homeassistant";
    public string? ConfigTabDisplayName => "Home Assistant";
    public string? ConfigPartialView => "ConfigUI/_HomeAssistant";

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "homeassistant", "homeassistant_url", "homeassistant_token"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => ["homeassistant"];

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date)
    {
        var displayService = services.GetRequiredService<IDisplayService>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        return new HomeAssistantComponent(displayService, httpClientFactory, memoryCache, loggerFactory);
    }
}
