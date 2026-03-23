using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Modules.Builtin;

public class WeatherForecastModule : IPortalModule
{
    public string ModuleId => "weather_forecast";
    public string? ConfigTabDisplayName => null;
    public string? ConfigPartialView => null;

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "weather_forecast_display_rows"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => [];

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date) => null;
}
