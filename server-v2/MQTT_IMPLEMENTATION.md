# MQTT Integration Implementation

## Overview
This implementation adds MQTT integration to the Portal Calendar Server, equivalent to the Perl implementation in `PortalCalendar::Integration::MQTT`.

## Files Created/Modified

### New Files
1. **PortalCalendarServer\Services\Integrations\Interfaces\IMqttService.cs**
   - Interface for MQTT service
   - Defines methods for publishing sensor data and disconnecting

2. **PortalCalendarServer\Services\Integrations\MqttService.cs**
   - Full implementation of MQTT service
   - Uses MQTTnet library for MQTT communication
   - Supports Home Assistant MQTT Discovery protocol
   - Publishes sensor data with proper device information and metadata

### Modified Files
1. **PortalCalendarServer\PortalCalendarServer.csproj**
   - Added MQTTnet package reference (version 4.3.7.1207)

2. **PortalCalendarServer\Program.cs**
   - Registered `IMqttService` and `MqttService` in dependency injection container

3. **PortalCalendarServer\Controllers\ApiController.cs**
   - Added `IMqttService` dependency
   - Integrated MQTT updates in the `Config` endpoint
   - Publishes all sensor data (voltage, battery, sleep time, etc.) to MQTT broker

## Features

### Home Assistant MQTT Discovery
The implementation supports Home Assistant MQTT Discovery with proper device configuration:
- Device manufacturer, model, identifiers
- Firmware version tracking
- Proper entity categorization (diagnostic sensors)
- Device classes (voltage, battery, duration, timestamp, enum)
- State classes (measurement)
- Units of measurement
- Custom icons

### Supported Sensors
The following sensors are published to MQTT:
- `voltage` - Current voltage with device class "voltage"
- `battery_percent` - Battery percentage with device class "battery"
- `voltage_raw` - Raw ADC value
- `min_voltage` - Minimum voltage threshold
- `max_voltage` - Maximum voltage threshold
- `min_linear_voltage` - Minimum linear voltage
- `max_linear_voltage` - Maximum linear voltage
- `last_visit` - Last connection timestamp
- `sleep_time` - Sleep duration in seconds
- `reset_reason` - Device reset reason (enum)
- `wakeup_reason` - Device wakeup reason (enum)

### Configuration
The MQTT service reads the following configuration values from the display config:
- `mqtt` - Enable/disable MQTT (boolean)
- `mqtt_server` - MQTT broker address (format: "host" or "host:port")
- `mqtt_username` - MQTT username (optional)
- `mqtt_password` - MQTT password (optional)
- `mqtt_topic` - Unique device identifier for MQTT topics

### MQTT Topics Structure
- **Config topic**: `homeassistant/{component}/{topic}/{key}/config`
- **State topic**: `epapers/{topic}/state/{key}`

Both topics use retained messages for reliability.

## Differences from Perl Implementation

1. **Async/Await Pattern**: The C# implementation uses async/await pattern for better scalability
2. **Dependency Injection**: Uses .NET's built-in DI instead of Perl's object system
3. **Strongly Typed**: C# provides compile-time type safety
4. **IAsyncDisposable**: Proper resource cleanup using .NET's disposal pattern
5. **Scoped Service**: MqttService is registered as scoped, with connection management per request

## Testing

To test the MQTT integration:
1. Configure MQTT settings in the display configuration
2. Ensure an MQTT broker is running (e.g., Mosquitto)
3. Monitor MQTT topics using an MQTT client:
   ```bash
   mosquitto_sub -h localhost -t "homeassistant/#" -v
   mosquitto_sub -h localhost -t "epapers/#" -v
   ```
4. Trigger the `/api/config` endpoint from a device
5. Verify that sensor data appears in Home Assistant (if configured)

## Future Enhancements

Potential improvements for future iterations:
- Connection pooling for multiple displays
- Retry logic with exponential backoff
- MQTT connection state monitoring
- Support for MQTT over TLS/SSL
- Configurable QoS levels
- Batch publishing for multiple sensors
