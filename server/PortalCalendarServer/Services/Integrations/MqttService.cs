using Microsoft.Extensions.Caching.Memory;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services.Caches;
using System.Text.Json;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Service for publishing MQTT messages with Home Assistant MQTT Discovery support
/// </summary>
public class MqttService(
    ILogger<MqttService> loggerParam,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    IDatabaseCacheServiceFactory databaseCacheFactory,
    IDisplayService displayService,
    CalendarContext context)
    : IntegrationServiceBase(loggerParam, httpClientFactory, memoryCache, databaseCacheFactory, context), IMqttService, IAsyncDisposable
{
    private new readonly ILogger<MqttService> logger = loggerParam;
    private IMqttClient? _mqttClient;
    private bool _isConnected = false;

    public override bool IsConfigured(Display display)
    {
        if (display == null)
        {
            return false;
        }

        var server = displayService.GetConfig(display, "mqtt_server");
        var username = displayService.GetConfig(display, "mqtt_username");
        var password = displayService.GetConfig(display, "mqtt_password");

        return !string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
    }

    private async Task<IMqttClient> GetConnectedClientAsync(Display display)
    {
        if (_mqttClient != null && _isConnected)
        {
            return _mqttClient;
        }

        if (!IsConfigured(display))
        {
            throw new InvalidOperationException("MQTT integration is not properly configured");
        }

        var server = displayService.GetConfig(display, "mqtt_server");
        var username = displayService.GetConfig(display, "mqtt_username");
        var password = displayService.GetConfig(display, "mqtt_password");

        // Parse server:port
        var serverParts = server!.Split(':');
        var host = serverParts[0];
        var port = serverParts.Length > 1 && int.TryParse(serverParts[1], out var p) ? p : 1883;

        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithCleanSession();

        if (!string.IsNullOrWhiteSpace(username))
        {
            optionsBuilder.WithCredentials(username, password);
        }

        var options = optionsBuilder.Build();

        try
        {
            await _mqttClient.ConnectAsync(options);
            _isConnected = true;
            logger.LogDebug("Connected to MQTT broker at {Server}:{Port}", host, port);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to MQTT broker at {Server}:{Port}", host, port);
            throw;
        }

        return _mqttClient;
    }

    public async Task PublishSensorAsync(Display display, string key, object? value, bool forced = false)
    {
        if (!displayService.GetConfigBool(display, "mqtt"))
        {
            return;
        }

        try
        {
            var client = await GetConnectedClientAsync(display);
            var topic = displayService.GetConfig(display, "mqtt_topic");

            if (string.IsNullOrWhiteSpace(topic))
            {
                logger.LogWarning("MQTT topic not configured for display {DisplayId}", display.Id);
                return;
            }

            var haDetail = GetHomeAssistantDetails(key);
            var component = haDetail.GetValueOrDefault("component", "sensor");

            var configTopic = $"homeassistant/{component}/{topic}/{key}/config";
            var stateTopic = $"epapers/{topic}/state/{key}";

            // Publish Home Assistant discovery config
            var configPayload = new Dictionary<string, object?>
            {
                ["state_topic"] = stateTopic,
                ["device"] = new Dictionary<string, object?>
                {
                    ["manufacturer"] = "Michal",
                    ["model"] = "Portal calendar ePaper",
                    ["identifiers"] = new[] { topic },
                    ["name"] = topic,
                    ["hw_version"] = "1.0",
                    ["sw_version"] = display.Firmware ?? "unknown"
                },
                ["enabled_by_default"] = true,
                ["force_update"] = forced,
                ["name"] = key,
                ["unique_id"] = $"{topic}_{key}"
            };

            // Add optional fields if present
            if (haDetail.TryGetValue("entity_category", out var entityCategory) && entityCategory != null)
            {
                configPayload["entity_category"] = entityCategory;
            }
            if (haDetail.TryGetValue("device_class", out var deviceClass) && deviceClass != null)
            {
                configPayload["device_class"] = deviceClass;
            }
            if (haDetail.TryGetValue("state_class", out var stateClass) && stateClass != null)
            {
                configPayload["state_class"] = stateClass;
            }
            if (haDetail.TryGetValue("unit_of_measurement", out var unit) && unit != null && !string.IsNullOrWhiteSpace(unit.ToString()))
            {
                configPayload["unit_of_measurement"] = unit;
            }
            if (haDetail.TryGetValue("icon", out var icon) && icon != null)
            {
                configPayload["icon"] = icon;
            }

            var configJson = JsonSerializer.Serialize(configPayload);
            var configMessage = new MqttApplicationMessageBuilder()
                .WithTopic(configTopic)
                .WithPayload(configJson)
                .WithRetainFlag()
                .Build();

            await client.PublishAsync(configMessage);
            logger.LogDebug("Published retained config topic {ConfigTopic}: {ConfigJson}", configTopic, configJson);

            // Publish state
            var stateValue = value?.ToString() ?? string.Empty;
            var stateMessage = new MqttApplicationMessageBuilder()
                .WithTopic(stateTopic)
                .WithPayload(stateValue)
                .WithRetainFlag()
                .Build();

            await client.PublishAsync(stateMessage);
            logger.LogDebug("Published retained state topic {StateTopic}: {StateValue}", stateTopic, stateValue);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish MQTT message for key {Key}", key);
        }
    }

    private static Dictionary<string, object?> GetHomeAssistantDetails(string key)
    {
        var details = new Dictionary<string, Dictionary<string, object?>>
        {
            ["voltage"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["device_class"] = "voltage",
                ["state_class"] = "measurement",
                ["unit_of_measurement"] = "V",
                ["icon"] = "mdi:battery"
            },
            ["voltage_raw"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["state_class"] = "measurement",
                ["unit_of_measurement"] = "ADC_raw_units",
                ["icon"] = "mdi:battery"
            },
            ["min_voltage"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["device_class"] = "voltage",
                ["state_class"] = "measurement",
                ["unit_of_measurement"] = "V",
                ["icon"] = "mdi:battery-outline"
            },
            ["max_voltage"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["device_class"] = "voltage",
                ["state_class"] = "measurement",
                ["unit_of_measurement"] = "V",
                ["icon"] = "mdi:battery"
            },
            ["min_linear_voltage"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["device_class"] = "voltage",
                ["state_class"] = "measurement",
                ["unit_of_measurement"] = "V",
                ["icon"] = "mdi:battery-outline"
            },
            ["max_linear_voltage"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["device_class"] = "voltage",
                ["state_class"] = "measurement",
                ["unit_of_measurement"] = "V",
                ["icon"] = "mdi:battery"
            },
            ["battery_percent"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["device_class"] = "battery",
                ["state_class"] = "measurement",
                ["unit_of_measurement"] = "%"
            },
            ["sleep_time"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["device_class"] = "duration",
                ["state_class"] = "measurement",
                ["unit_of_measurement"] = "s",
                ["icon"] = "mdi:sleep"
            },
            ["last_visit"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["device_class"] = "timestamp",
                ["state_class"] = null,
                ["unit_of_measurement"] = "",
                ["icon"] = "mdi:clock-time-four"
            },
            ["reset_reason"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["device_class"] = "enum",
                ["icon"] = "mdi:chip"
            },
            ["wakeup_reason"] = new()
            {
                ["component"] = "sensor",
                ["entity_category"] = "diagnostic",
                ["device_class"] = "enum",
                ["icon"] = "mdi:sleep-off"
            }
        };

        return details.TryGetValue(key, out var detail) ? detail : new Dictionary<string, object?>();
    }

    public async Task DisconnectAsync()
    {
        if (_mqttClient != null && _isConnected)
        {
            try
            {
                await _mqttClient.DisconnectAsync();
                _isConnected = false;
                logger.LogDebug("Disconnected from MQTT broker");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disconnecting from MQTT broker");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _mqttClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
