using PortalCalendarServer.Models.Entities;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Service interface for publishing MQTT messages to Home Assistant or other MQTT brokers
/// </summary>
public interface IMqttService : IIntegrationService
{
    /// <summary>
    /// Publish a sensor value to MQTT with Home Assistant discovery support
    /// </summary>
    /// <param name="display">The display to publish data for</param>
    /// <param name="key">The sensor key/name</param>
    /// <param name="value">The sensor value</param>
    /// <param name="forced">Whether to force update even if value hasn't changed</param>
    Task PublishSensorAsync(Display display, string key, object? value, bool forced = false);

    /// <summary>
    /// Disconnect from MQTT broker
    /// </summary>
    Task DisconnectAsync();
}
