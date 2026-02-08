using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Models;

namespace PortalCalendarServer.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly CalendarContext _context;
    private readonly ILogger<ApiController> _logger;

    public ApiController(CalendarContext context, ILogger<ApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Helper to get display by MAC address
    private async Task<Display?> GetDisplayByMacAsync(string? mac)
    {
        if (string.IsNullOrWhiteSpace(mac))
        {
            return null;
        }

        mac = mac.ToLowerInvariant();
        var display = await _context.Displays
            .Include(d => d.Configs)
            .FirstOrDefaultAsync(d => d.Mac == mac);

        if (display == null)
        {
            _logger.LogWarning("Display with MAC [{Mac}] not found", mac);
        }

        return display;
    }

    // GET /api/ping
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { status = "ok" });
    }

    // GET /api/health
    [HttpGet("health")]
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
                BorderLeft = 0
            };

            _context.Displays.Add(display);
            await _context.SaveChangesAsync();

            // Set default theme
            display.SetConfig(_context, "theme", "default");
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
        display.SetConfig(_context, "_last_visit", DateTime.UtcNow.ToString("O"));

        // Store voltage and diagnostic data
        display.SetConfig(_context, "_last_voltage_raw", adc ?? voltage_raw ?? string.Empty);
        display.SetConfig(_context, "_last_voltage", v ?? string.Empty);
        display.SetConfig(_context, "_min_voltage", vmin ?? string.Empty);
        display.SetConfig(_context, "_max_voltage", vmax ?? string.Empty);
        display.SetConfig(_context, "_min_linear_voltage", vlmin ?? string.Empty);
        display.SetConfig(_context, "_max_linear_voltage", vlmax ?? string.Empty);
        display.SetConfig(_context, "_reset_reason", reset ?? string.Empty);
        display.SetConfig(_context, "_wakeup_reason", wakeup ?? string.Empty);

        await _context.SaveChangesAsync();

        // Calculate next wakeup time
        var (nextWakeup, sleepInSeconds, schedule) = display.NextWakeupTime();
        _logger.LogInformation(
            "Next wakeup at {NextWakeup} (in {SleepSeconds} seconds) according to crontab schedule '{Schedule}'",
            nextWakeup, sleepInSeconds, schedule);

        // TODO: Update MQTT values

        // TODO: Handle missed connects and notifications

        var response = new
        {
            sleep = sleepInSeconds,
            battery_percent = display.BatteryPercent(),
            ota_mode = display.GetConfigBool("ota_mode")
        };

        return Ok(response);
    }

    // GET /api/calendar/bitmap?mac=XX:XX:XX:XX:XX:XX&rotate=0&flip=&format=png&...
    [HttpGet("calendar/bitmap")]
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

        gamma ??= display.Gamma;
        colors ??= display.NumColors();

        // TODO: Implement bitmap generation
        // This will require porting the Perl image generation logic
        // For now, return a placeholder response
        _logger.LogWarning("Bitmap generation not yet implemented");

        return NotFound(new { error = "Bitmap generation not yet implemented" });
    }

    // GET /api/calendar/bitmap/epaper?mac=XX:XX:XX:XX:XX:XX&web_format=false&preview_colors=false
    [HttpGet("calendar/bitmap/epaper")]
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