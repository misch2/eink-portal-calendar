using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services;

namespace PortalCalendarServer.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly CalendarContext _context;
    private readonly ILogger<ApiController> _logger;
    private readonly DisplayService _displayService;
    private readonly PageGeneratorService _pageGeneratorService;
    private readonly ThemeService _themeService;

    public ApiController(CalendarContext context, ILogger<ApiController> logger, DisplayService displayService, PageGeneratorService pageGeneratorService, ThemeService themeService, Web2PngService web2PngService, ColorTypeRegistry colorTypeRegistry)
    {
        _context = context;
        _logger = logger;
        _displayService = displayService;
        _pageGeneratorService = pageGeneratorService;
        _themeService = themeService;
    }

    // Helper to get display by MAC address
    private async Task<Display?> GetDisplayByMacAsync(string? mac)
    {
        if (string.IsNullOrWhiteSpace(mac))
        {
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


    // GET /api/config?mac=XX:XX:XX:XX:XX:XX&fw=1.0&w=800&h=480&c=BW&adc=2048&v=4.2&...
    [HttpGet("config")]
    [Tags("Device API", "Display Configuration")]
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

            // TODO: Enqueue regenerate_image task
        }
        else
        {
            // Update existing display
            if (!string.IsNullOrEmpty(fw))
            {
                display.Firmware = fw;
                _context.Update(display);
                await _context.SaveChangesAsync();
            }
        }


        // Update last visit timestamp
        _displayService.SetConfig(display, "_last_visit", DateTime.UtcNow.ToString("O"));

        // Store voltage and diagnostic data
        _displayService.SetConfig(display, "_last_voltage_raw", adc ?? voltage_raw ?? string.Empty);
        _displayService.SetConfig(display, "_last_voltage", v ?? string.Empty);
        _displayService.SetConfig(display, "_min_voltage", vmin ?? string.Empty);
        _displayService.SetConfig(display, "_max_voltage", vmax ?? string.Empty);
        _displayService.SetConfig(display, "_min_linear_voltage", vlmin ?? string.Empty);
        _displayService.SetConfig(display, "_max_linear_voltage", vlmax ?? string.Empty);
        _displayService.SetConfig(display, "_reset_reason", reset ?? string.Empty);
        _displayService.SetConfig(display, "_wakeup_reason", wakeup ?? string.Empty);

        await _displayService.SaveChangesAsync();

        // Calculate next wakeup time
        var wakeupInfo = display.NextWakeupTime();
        _logger.LogInformation(
            "Next wakeup at {NextWakeup} (in {SleepSeconds} seconds) according to crontab schedule '{Schedule}'",
            wakeupInfo.NextWakeup, wakeupInfo.SleepInSeconds, wakeupInfo.Schedule);

        // TODO: Update MQTT values

        // TODO: Handle missed connects and notifications

        var response = new
        {
            sleep = wakeupInfo.SleepInSeconds,
            battery_percent = display.BatteryPercent(),
            ota_mode = _displayService.GetConfigBool(display, "ota_mode")
        };

        return Ok(response);
    }

    // GET /api/calendar/bitmap?mac=XX:XX:XX:XX:XX:XX&rotate=0&flip=&format=png&...
    [HttpGet("calendar/bitmap")]
    [Tags("Device API", "Image Generation")]
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

        _pageGeneratorService.GenerateImageFromWeb(display); // FIXME pre-generate

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

        var bitmap = _pageGeneratorService.GenerateBitmap(display, bitmapOptions);

        if (bitmap == null)
        {
            return StatusCode(500, new { error = "Failed to generate bitmap" });
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

    // GET /api/calendar/bitmap/epaper?mac=XX:XX:XX:XX:XX:XX&web_format=false&preview_colors=false
    [HttpGet("calendar/bitmap/epaper")]
    [Tags("Device API", "Image Generation")]
    public async Task<IActionResult> BitmapEpaper(
        [FromQuery] string? mac,
        [FromQuery] bool web_format = false,
        [FromQuery] bool preview_colors = false)
    {
        var display = await GetDisplayByMacAsync(mac);
        if (display == null)
        {
            return NotFound(new { error = "Display not found" });
        }

        var rotate = web_format ? 0 : display.Rotation;
        var format = web_format ? "png" : "epaper_native";

        // TODO: Implement epaper bitmap generation
        // This will require porting the Perl image generation logic
        _logger.LogWarning("E-paper bitmap generation not yet implemented");

        return NotFound(new { error = "E-paper bitmap generation not yet implemented" });
    }
}