using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for MQTT integration.
/// Provides only a config tab — no page-generator component.
/// </summary>
public class MqttModule : IPortalModule
{
    public string ModuleId => "mqtt";
    public string? ConfigTabDisplayName => "MQTT";
    public string? ConfigPartialView => "ConfigUI/_Mqtt";

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "mqtt", "mqtt_server", "mqtt_username", "mqtt_password", "mqtt_topic"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => ["mqtt"];

    public object? CreateComponent(IServiceProvider services, Display display, DateTime date) => null;
}
