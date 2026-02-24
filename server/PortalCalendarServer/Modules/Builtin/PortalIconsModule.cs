using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for the Portal-themed icons displayed on the calendar face.
/// No config tab — icon settings are part of the Main config tab.
/// </summary>
public class PortalIconsModule : IPortalModule
{
    public string ModuleId => "portalicons";
    public string? ConfigTabDisplayName => null;
    public string? ConfigPartialView => null;

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "totally_random_icon", "min_random_icons", "max_random_icons", "max_icons_with_calendar"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => ["totally_random_icon"];

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date)
    {
        var displayService = services.GetRequiredService<IDisplayService>();
        return new PortalIconsComponent(displayService);
    }
}
