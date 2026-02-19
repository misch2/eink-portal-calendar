using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
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

    public ApiController(
        CalendarContext context,
        ILogger<ApiController> logger,
        IDisplayService displayService,
        PageGeneratorService pageGeneratorService,
        ThemeService themeService,
        IWeb2PngService web2PngService,
        ColorTypeRegistry colorTypeRegistry,
        IMqttService mqttService)
    {
        _context = context;
        _logger = logger;
        _displayService = displayService;
        _pageGeneratorService = pageGeneratorService;
        _themeService = themeService;
        _mqttService = mqttService;
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
        var display = await _context.Displays.FirstOrDefaultAsync(d => d.Mac == mac);

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
    [HttpGet("~/config")] // FIXME remove later, keep for backward compatibility
    [Tags("Device API")]
    public async Task<IActionResult> Config(
    [FromQuery] string? mac,
    [FromQuery] string? fw,
    [FromQuery] int? w,
    [FromQuery] int? h,
    [FromQuery] string? c,
    [FromQuery] string? adc,
    [FromQuery] string? voltage_raw,
    [FromQuery] string? v,
    [FromQuery] string? vmin,
    [FromQuery] string? vmax,
    [FromQuery] string? vlmin,
    [FromQuery] string? vlmax,
    [FromQuery] string? reset,
    [FromQuery] string? wakeup)
    {
        if (string.IsNullOrWhiteSpace(mac))
        {
            return BadRequest(new { error = "MAC address is required" });
        }

        var display = await GetDisplayByMacAsync(mac);

        if (display == null)
        {
            var defaultTheme = await _themeService.GetDefaultThemeAsync();

            // Create new display
            display = new Display
            {
                Mac = mac.ToLowerInvariant(),
                Name = $"New display with MAC {mac.ToUpperInvariant()} added on {DateTime.UtcNow}",
                Width = w ?? 800,
                Height = h ?? 480,
                ColorType = c ?? "BW",
                Firmware = fw ?? string.Empty,
                Rotation = 0,
                Gamma = 2.2,
                BorderTop = 0,
                BorderRight = 0,
                BorderBottom = 0,
                BorderLeft = 0,
                ThemeId = defaultTheme.Id
            };

            _context.Displays.Add(display);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New display created with MAC {Mac}, ID: {Id}", mac, display.Id);

            _displayService.EnqueueImageRegenerationRequest(display);
        }
        else
        {
            // Update existing display
            if (!string.IsNullOrWhiteSpace(fw))
            {
                display.Firmware = fw;
                _context.Update(display);
                await _context.SaveChangesAsync();
            }
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
        _displayService.SetConfig(display, "_last_voltage_raw", adc ?? voltage_raw ?? string.Empty);
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
            wakeupInfo.NextWakeup, wakeupInfo.SleepInSeconds, wakeupInfo.Schedule);

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
            ota_mode = _displayService.GetConfigBool(display, "ota_mode")
        };

        return Ok(response);
    }

    // GET /api/device/bitmap?mac=XX:XX:XX:XX:XX:XX&rotate=0&flip=&format=png&...
    [HttpGet("device/bitmap")]
    [HttpGet("~/calendar/bitmap")] // FIXME remove later, keep for backward compatibility
    [Tags("Device API")]
    public async Task<IActionResult> Bitmap(
        [FromQuery] string? mac,
        [FromQuery] int rotate = 0,
        [FromQuery] string flip = "",
        [FromQuery] double? gamma = null,
        [FromQuery] int? colors = null,
        [FromQuery] string colormap_name = "none",
        [FromQuery] string format = "png",
        [FromQuery] bool preview_colors = false)
    {
        var display = await GetDisplayByMacAsync(mac);
        if (display == null)
        {
            return NotFound(new { error = "Display not found" });
        }

        var colortype = _displayService.GetColorType(display);

        gamma ??= display.Gamma;
        colors ??= colortype?.NumColors;

        var color_palette = colortype?.ColorPalette(preview_colors) ?? new List<string>();
        if (color_palette.Count == 0)
        {
            colormap_name = "webmap";
        }

        var bitmapOptions = new BitmapOptions
        {
            Rotate = rotate,
            Flip = flip,
            Gamma = gamma!.Value,
            NumColors = colors!.Value,
            ColormapName = colormap_name,
            ColormapColors = color_palette,
            Format = format,
            DisplayType = display.ColorType
        };

        var bitmap = _pageGeneratorService.ConvertStoredBitmap(display, bitmapOptions);

        return ReturnBitmap(bitmap);
    }


    // GET /api/device/bitmap/epaper?mac=XX:XX:XX:XX:XX:XX&web_format=false&preview_colors=false
    [HttpGet("device/bitmap/epaper")]
    [HttpGet("~/calendar/bitmap/epaper")] // FIXME remove later, keep for backward compatibility
    [Tags("Device API")]
    public async Task<IActionResult> BitmapEpaper(
        [FromQuery] string? mac,
        [FromQuery] bool web_format = false,
        [FromQuery] bool preview_colors = false
        )
    {
        var display = await GetDisplayByMacAsync(mac);
        if (display == null)
        {
            return NotFound(new { error = "Display not found" });
        }

        var colortype = _displayService.GetColorType(display);

        var rotate = display.Rotation;
        var numcolors = colortype!.NumColors;
        var format = "epaper_native";

        var color_palette = colortype?.ColorPalette(preview_colors) ?? new List<string>();
        var colormap_name = color_palette.Count > 0 ? "none" : "webmap";

        if (web_format)
        {
            format = "png";
            rotate = 0;
        }

        var bitmapOptions = new BitmapOptions
        {
            Rotate = rotate,
            Flip = "",
            Gamma = display!.Gamma.Value,
            NumColors = numcolors,
            ColormapName = colormap_name,
            ColormapColors = color_palette,
            Format = format,
            DisplayType = display.ColorType
        };

        var bitmap = _pageGeneratorService.ConvertStoredBitmap(display, bitmapOptions);

        return ReturnBitmap(bitmap);
    }

    private IActionResult ReturnBitmap(BitmapResult bitmap)
    {
        if (bitmap == null)
        {
            return StatusCode(500, new { error = "Failed to convert bitmap" });
        }

        // Return the bitmap data with proper headers
        if (bitmap.Headers != null)
        {
            foreach (var header in bitmap.Headers)
            {
                Response.Headers.Append(header.Key, header.Value);
            }
        }

        return File(bitmap.Data, bitmap.ContentType);
    }


}

