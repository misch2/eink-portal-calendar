using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module backing the "Main" config tab (locale, display title, schedules).
/// Keys shared with other modules (lat/lon/alt) are intentionally duplicated
/// in those modules' <see cref="IPortalModule.OwnedConfigKeys"/>; deduplication
/// happens automatically via <see cref="ModuleRegistry.AllOwnedConfigKeys"/>.
/// </summary>
public class MainConfigModule : IPortalModule
{
    public string ModuleId => "main";
    public string? ConfigTabDisplayName => "Main";
    public string? ConfigPartialView => "ConfigUI/_Main";

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "display_title", "timezone", "date_culture", "lat", "lon", "alt"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => [];

    public object? CreateComponent(IServiceProvider services, Display display, DateTime date) => null;
}
