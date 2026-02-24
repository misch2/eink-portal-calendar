using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module backing the "ePaper display (client)" config tab.
/// Contains scheduling, alive-check, and OTA settings.
/// </summary>
public class ClientConfigModule : IPortalModule
{
    public string ModuleId => "client";
    public string? ConfigTabDisplayName => "ePaper display (client)";
    public string? ConfigPartialView => "ConfigUI/_Client";

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "ota_mode", "wakeup_schedule",
        "alive_check_safety_lag_minutes", "alive_check_minimal_failure_count"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => ["ota_mode"];

    public object? CreateComponent(IServiceProvider services, Display display, DateTime date) => null;
}
