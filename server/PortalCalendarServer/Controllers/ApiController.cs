using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Models.POCOs;
using PortalCalendarServer.Models.POCOs.Bitmap;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly CalendarContext _context;
    private readonly ILogger<ApiController> _logger;
    private readonly IDisplayService _displayService;
    private readonly PageGeneratorService _pageGeneratorService;
    private readonly ThemeService _themeService;
    private readonly IMqttService _mqttService;
    private readonly PairingModeService _pairingMode;

    private readonly static Version firmwareVersionWithApiKeySupport = new Version("2.3.0");

    public ApiController(
        CalendarContext context,
        ILogger<ApiController> logger,
        IDisplayService displayService,
        PageGeneratorService pageGeneratorService,
        ThemeService themeService,
        IWeb2PngService web2PngService,
        IMqttService mqttService,
        PairingModeService pairingMode)
    {
        _context = context;
        _logger = logger;
        _displayService = displayService;
        _pageGeneratorService = pageGeneratorService;
        _themeService = themeService;
        _mqttService = mqttService;
        _pairingMode = pairingMode;
    }

    private static string? GenerateApiKeyForDisplayIfNeeded(Display display)
    {
        var displayVersion = new Version(display.Firmware ?? "0.0.0");

        if (displayVersion >= firmwareVersionWithApiKeySupport)
        {
            if (string.IsNullOrWhiteSpace(display.ApiKey))
            {
                // Device supports API keys, but display doesn't have one assigned yet
                return Guid.NewGuid().ToString("D");
            }
            else
            {
                return display.ApiKey;
            }
        }

        // Device firmware doesn't support API keys
        return null;
    }

    // Helper to get display by MAC address
    private async Task<Display?> GetDisplayByMacAsync(string? mac)
    {
        if (string.IsNullOrWhiteSpace(mac))
        {
            _logger.LogWarning("MAC address is missing or empty");
            return null;
        }

        mac = mac.ToLowerInvariant();
        var display = await _context.Displays
            .Include(d => d.Configs)
            .SingleOrDefaultAsync(d => d.Mac == mac);

        if (display == null)
        {
            _logger.LogWarning("Display with MAC [{Mac}] not found", mac);
        }

        return display;
    }

    // GET /api/ping
    [HttpGet("ping")]
    [Tags("Health Checks")]
    public IActionResult Ping()
    {
        return Ok(new { status = "ok" });
    }

    // GET /api/health
    [HttpGet("health")]
    [Tags("Health Checks")]
    public IActionResult Health()
    {
        try
        {
            // Perform a simple database query to check connectivity
            var canConnect = _context.Displays.Any();
            if (!canConnect)
            {
                _logger.LogError("Database connection failed: unable to query Displays");
                return StatusCode(503, new { status = "unhealthy", error = "Database connection failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection failed: {Message}", ex.Message);
            return StatusCode(503, new { status = "unhealthy", error = "Database connection failed" });
        }

        return Ok(new { status = "healthy" });
    }

    // GET /api/device/config?mac=XX:XX:XX:XX:XX:XX&fw=1.0&w=800&h=480&c=BW&adc=2048&v=4.2&...
    [HttpGet("device/config")]
    [Tags("Device API")]
    [EnableRateLimiting("device-config")]
    public async Task<IActionResult> Config(
    [FromQuery] string? mac,
    [FromQuery] string? fw,
    [FromQuery] int? w,
    [FromQuery] int? h,
    [FromQuery] string? c,
    [FromQuery(Name = "rot")] int? rotation,
    [FromQuery(Name = "adc")] string? voltage_raw,
    [FromQuery] string? v,
    [FromQuery] string? vmin,
    [FromQuery] string? vmax,
    [FromQuery] string? vlmin,
    [FromQuery] string? vlmax,
    [FromQuery] string? reset,
    [FromQuery] string? wakeup,
    [FromQuery] string? key)
    {
        if (string.IsNullOrWhiteSpace(mac))
        {
            return BadRequest(new { error = "MAC address is required" });
        }

        var display = await GetDisplayByMacAsync(mac);

        // Key that will be sent back to the device (only set when a new key is assigned)
        string? newApiKey = null;

        if (display == null)
        {
            // Unknown device, only allow in pairing mode
            if (!_pairingMode.IsActive)
            {
                _logger.LogWarning("Rejected unknown MAC {Mac}, pairing mode is not active", mac);
                return Unauthorized(new { error = "Pairing mode is not active. Enable it in the admin UI to register new displays." });
            }

            var displayType = _context.DisplayTypes.FirstOrDefault(dt => dt.Code == c);
            if (displayType == null)
            {
                _logger.LogWarning("Invalid display type code '{Code}' for new display with MAC {Mac}", c, mac);
                return BadRequest(new { error = $"Invalid display type code '{c}'" });
            }
            var defaultColorVariant = _context.ColorVariants.FirstOrDefault(cv => cv.DisplayTypeCode == c);
            if (defaultColorVariant == null)
            {
                _logger.LogWarning("No color variant found for display type code '{Code}' when pairing new display with MAC {Mac}", c, mac);
                return BadRequest(new { error = $"No color variant found for display type code '{c}'" });
            }

            // Create new display
            display = new Display
            {
                Mac = mac.ToLowerInvariant(),
                Name = $"New display with MAC {mac.ToUpperInvariant()}",
                Width = w ?? 800,
                Height = h ?? 480,
                DisplayTypeCode = displayType.Code,
                ColorVariantCode = defaultColorVariant.Code,
                Firmware = fw ?? string.Empty,
                Rotation = (DisplayRotation)(rotation ?? (int)DisplayRotation.None),
                Gamma = 1.0,
                BorderTop = 0,
                BorderRight = 0,
                BorderBottom = 0,
                BorderLeft = 0,
                ThemeId = (await _themeService.GetDefaultThemeAsync()).Id
            };

            display.ApiKey = GenerateApiKeyForDisplayIfNeeded(display);
            if (display.ApiKey == null)
            {
                _logger.LogWarning("Device firmware {Firmware} does not support API keys for display with MAC {Mac}", fw, mac);
                return BadRequest(new { error = "Device firmware too old to support API keys." });
            }

            newApiKey = display.ApiKey;
            _logger.LogInformation("Assigned new API key to newly paired display with MAC {Mac}: {ApiKey}", mac, newApiKey);

            // Update display type before the single save
            if (!string.IsNullOrWhiteSpace(c))
                display.DisplayTypeCode = c;

            _context.Displays.Add(display);
            await _context.SaveChangesAsync();

            // Close pairing window, one device paired
            _pairingMode.Deactivate();
            _logger.LogInformation("New display paired: MAC {Mac}, ID: {Id}", mac, display.Id);

            // Generate the bitmap NOW so that it's available immediately on the first config request.
            try
            {
                await _pageGeneratorService.RenderDisplayImageAsync(display);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate initial bitmap for new display {DisplayId}", display.Id);
            }
        }
        else
        {
            // Existing display — update firmware and display type if provided
            if (!string.IsNullOrWhiteSpace(fw))
                display.Firmware = fw;

            // Check API key if display FW supports it.
            // Allows EXISTING old displays without keys to continue working but requires new devices to have a key assigned (see above).
            if (display.ApiKey == null)
            {
                // Existing display without a key (pre-pairing firmware or NVS cleared).
                // Grace period: assign a key now and return it so the device can store it.
                display.ApiKey = GenerateApiKeyForDisplayIfNeeded(display);
                if (display.ApiKey != null)
                {
                    newApiKey = display.ApiKey;
                    _logger.LogInformation("Assigned new API key to existing keyless display {DisplayId}: {ApiKey}", display.Id, newApiKey);
                }
            }
            else if (!string.Equals(key, display.ApiKey, StringComparison.Ordinal))
            {
                _logger.LogWarning("Invalid API key for display MAC {Mac}", mac);
                return Unauthorized(new { error = "Invalid API key" });
            }

            // Update display type
            if (!string.IsNullOrWhiteSpace(c))
                display.DisplayTypeCode = c;

            _context.Update(display);
            await _context.SaveChangesAsync();
        }

        // Update last visit timestamp
        _displayService.SetConfig(display, "_last_visit", DateTime.UtcNow.ToString("O"));

        // Handle missed connects - reset if there were any
        var missedConnects = _displayService.GetMissedConnects(display);
        if (missedConnects > 0)
        {
            // Check if we need to send an "unfrozen" notification
            var frozenNotificationSent = _displayService.GetConfigBool(display, "_frozen_notification_sent");
            if (frozenNotificationSent)
            {
                var lastVisit = _displayService.GetLastVisit(display);
                if (lastVisit.HasValue)
                {
                    var timeZone = _displayService.GetTimeZoneInfo(display);
                    var lastVisitLocal = TimeZoneInfo.ConvertTimeFromUtc(lastVisit.Value, timeZone);
                    var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                    var hoursSince = (int)((DateTime.UtcNow - lastVisit.Value).TotalHours + 0.5);
                    var message = $"Display '{display.Name}' (ID: {display.Id}) has reconnected!\n" +
                                  $"Was frozen since: {lastVisitLocal:yyyy-MM-dd HH:mm}\n" +
                                  $"Reconnected at: {nowLocal:yyyy-MM-dd HH:mm}\n" +
                                  $"Was offline for approximately {hoursSince} hours";

                    _logger.LogWarning("Display {DisplayId} ({DisplayName}) reconnected after being frozen: {Message}",
                                        display.Id, display.Name, message);

                    // Send unfrozen notification via Telegram if configured
                    if (_displayService.GetConfigBool(display, "telegram"))
                    {
                        var apiKey = _displayService.GetConfig(display, "telegram_api_key");
                        var chatId = _displayService.GetConfig(display, "telegram_chat_id");

                        if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(chatId))
                        {
                            try
                            {
                                // TODO: Implement Telegram notification sending
                                _logger.LogInformation("Would send Telegram unfrozen notification to chat {ChatId} for display {DisplayId}",
                                    chatId, display.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send Telegram unfrozen notification for display {DisplayId}", display.Id);
                            }
                        }
                    }

                    _displayService.SetConfig(display, "_frozen_notification_sent", "0");
                }
            }

            _displayService.ResetMissedConnectsCount(display);
        }

        // Store voltage and diagnostic data
        _displayService.SetConfig(display, "_last_voltage_raw", voltage_raw ?? string.Empty);
        _displayService.SetConfig(display, "_last_voltage", v ?? string.Empty);
        _displayService.SetConfig(display, "_min_voltage", vmin ?? string.Empty);
        _displayService.SetConfig(display, "_max_voltage", vmax ?? string.Empty);
        _displayService.SetConfig(display, "_min_linear_voltage", vlmin ?? string.Empty);
        _displayService.SetConfig(display, "_max_linear_voltage", vlmax ?? string.Empty);
        _displayService.SetConfig(display, "_reset_reason", reset ?? string.Empty);
        _displayService.SetConfig(display, "_wakeup_reason", wakeup ?? string.Empty);
        await _context.SaveChangesAsync();

        // Calculate next wakeup time
        var wakeupInfo = _displayService.GetNextWakeupTime(display);
        _logger.LogInformation(
            "Next wakeup at {NextWakeup} (in {SleepSeconds} seconds) according to crontab schedule '{Schedule}'",
            wakeupInfo.NextWakeup.ToString("O"), wakeupInfo.SleepInSeconds, wakeupInfo.Schedule);

        // Update MQTT values
        await _mqttService.PublishSensorAsync(display, "voltage", _displayService.GetVoltage(display), true);
        await _mqttService.PublishSensorAsync(display, "battery_percent", _displayService.GetBatteryPercent(display), true);
        await _mqttService.PublishSensorAsync(display, "voltage_raw", _displayService.GetConfig(display, "_last_voltage_raw"), true);
        await _mqttService.PublishSensorAsync(display, "min_voltage", _displayService.GetConfig(display, "_min_voltage"), true);
        await _mqttService.PublishSensorAsync(display, "max_voltage", _displayService.GetConfig(display, "_max_voltage"), true);
        await _mqttService.PublishSensorAsync(display, "min_linear_voltage", _displayService.GetConfig(display, "_min_linear_voltage"), true);
        await _mqttService.PublishSensorAsync(display, "max_linear_voltage", _displayService.GetConfig(display, "_max_linear_voltage"), true);
        await _mqttService.PublishSensorAsync(display, "last_visit", DateTime.UtcNow.ToString("O"));
        await _mqttService.PublishSensorAsync(display, "sleep_time", wakeupInfo.SleepInSeconds, true);
        await _mqttService.PublishSensorAsync(display, "reset_reason", _displayService.GetConfig(display, "_reset_reason"), true);
        await _mqttService.PublishSensorAsync(display, "wakeup_reason", _displayService.GetConfig(display, "_wakeup_reason"), true);

        // Final message (workaround for wakeup_reason not being updated)
        await _mqttService.PublishSensorAsync(display, "last_visit", DateTime.UtcNow.ToString("O"));
        await _mqttService.DisconnectAsync();

        var response = new
        {
            sleep = wakeupInfo.SleepInSeconds,
            battery_percent = _displayService.GetBatteryPercent(display),
            ota_mode = _displayService.GetConfigBool(display, "ota_mode"),
            // Only present when a new key is assigned (first pairing or grace period upgrade).
            // The device must persist this value to NVS and send it on all future requests.
            api_key = newApiKey
        };

        return Ok(response);
    }

    // GET /api/device/bitmap/epaper?mac=XX:XX:XX:XX:XX:XX[&fmt=1]
    [HttpGet("device/bitmap/epaper")]
    [Tags("Device API")]
    [EnableRateLimiting("device-bitmap")]
    public async Task<IActionResult> BitmapEpaper(
        [FromQuery] string? mac,
        [FromQuery] int fmt = 1,
        [FromQuery] string? key = null)
    {
        var display = await GetDisplayByMacAsync(mac);
        if (display == null)
        {
            return NotFound(new { error = "Display not found" });
        }

        if (display.ApiKey != null && !string.Equals(key, display.ApiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Invalid API key for bitmap request, display MAC {Mac}", mac);
            return Unauthorized(new { error = "Invalid API key" });
        }

        var bitmap = _displayService.ConvertExistingRawBitmap(
            displayId: display.Id,
            format: fmt == 2 ? OutputFormat.EpaperSpecificV2 : OutputFormat.EpaperSpecificV1,
            rotate: null,
            flip: null,
            cachePostfix: "device"
            );

        if (bitmap.ErrorMessage != null)
        {
            return NotFound(new { error = bitmap.ErrorMessage });
        }

        return this.ReturnBitmap(bitmap);
    }
}
