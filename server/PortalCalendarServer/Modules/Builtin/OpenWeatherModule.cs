using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for OpenWeather integration.
/// Provides only a config tab — the actual component is part of <see cref="MetNoWeatherModule"/>
/// since both share the same <see cref="Services.PageGeneratorComponents.WeatherComponent"/>.
/// </summary>
public class OpenWeatherModule : IPortalModule
{
    public string ModuleId => "openweather";
    public string? ConfigTabDisplayName => "OpenWeather";
    public string? ConfigPartialView => "ConfigUI/_OpenWeather";

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "openweather", "openweather_api_key", "openweather_lang"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => ["openweather"];

    // Component is provided by MetNoWeatherModule (shared WeatherComponent)
    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date) => null;
}
