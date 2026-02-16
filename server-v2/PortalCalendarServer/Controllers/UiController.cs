using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Services;

namespace PortalCalendarServer.Controllers;

[Controller]
public class UiController(
    CalendarContext context,
    ILogger<UiController> logger,
    IWebHostEnvironment environment,
    PageGeneratorService pageGeneratorService,
    IDisplayService displayService,
    ThemeService themeService
    ) : Controller
{
    private readonly CalendarContext _context = context;
    private readonly ILogger<UiController> _logger = logger;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly PageGeneratorService _pageGeneratorService = pageGeneratorService;
    private readonly IDisplayService _displayService = displayService;
    private readonly ThemeService _themeService = themeService;

    // Config parameter names that can be saved from the UI
    private static readonly string[] ConfigUiParameters =
    [
        // FIXME this is not ideal, every config class should have a list of its own parameters, and we should only save those instead of this big hardcoded list
        "alive_check_safety_lag_minutes", "alive_check_minimal_failure_count", "alt", "date_culture", "display_title",
        "googlefit", "googlefit_auth_callback", "googlefit_client_id", "googlefit_client_secret",
        "lat", "lon", "max_icons_with_calendar", "max_random_icons", "metnoweather",
        "metnoweather_granularity_hours", "min_random_icons", "mqtt", "mqtt_password", "mqtt_server",
        "mqtt_topic", "mqtt_username", "openweather", "openweather_api_key", "openweather_lang",
        "ota_mode", "telegram", "telegram_api_key", "telegram_chat_id", "theme_id", "timezone",
        "totally_random_icon", "wakeup_schedule", "web_calendar_ics_url1", "web_calendar_ics_url2",
        "web_calendar_ics_url3", "web_calendar1", "web_calendar2", "web_calendar3"
    ];

    // GET /
    [HttpGet("/")]
    public async Task<IActionResult> SelectDisplay()
    {
        var displays = await _context.Displays
            .OrderBy(d => d.Id)
            .ToListAsync();

        ViewData["NavLink"] = "index";
        ViewData["Title"] = "Displays";

        return View("DisplayList", displays);
    }

    // GET /home/{display_number}
    [HttpGet("/home/{displayNumber:int}")]
    public IActionResult Home(int displayNumber)
    {
        var display = _displayService.GetDisplayById(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        ViewData["NavLink"] = "home";
        ViewData["Title"] = $"Display {display.Name}";
        ViewBag.Display = display; // for global layout

        return View("Index", display);
    }

    // GET /test/{display_number}
    [HttpGet("/test/{displayNumber:int}")]
    public IActionResult Test(int displayNumber)
    {
        var display = _displayService.GetDisplayById(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        ViewData["NavLink"] = "compare";
        ViewData["Title"] = $"Test - {display.Name}";
        ViewBag.Display = display; // for global layout

        return View("Test", display);
    }

    // POST /delete/{display_number}
    [HttpPost("/delete/{displayNumber:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDisplay(int displayNumber)
    {
        var display = _displayService.GetDisplayById(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        _context.Configs.RemoveRange(_context.Configs.Where(c => c.DisplayId == display.Id));
        if (display.Theme != null)
        {
            _context.Themes.Remove(display.Theme);
        }
        _context.Displays.Remove(display);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Display deleted: {DisplayId}", displayNumber);

        TempData["Message"] = "Display deleted.";
        return RedirectToAction(nameof(SelectDisplay));
    }

    // GET /calendar/{display_number}/html
    [HttpGet("/calendar/{displayNumber:int}/html")]
    public IActionResult CalendarHtmlDefaultDate(int displayNumber, [FromQuery] bool preview_colors = false)
    {
        return CalendarHtmlSpecificDate(displayNumber, DateTime.UtcNow, preview_colors);
    }

    // GET /calendar/{display_number}/html/{date}
    [HttpGet("/calendar/{displayNumber:int}/html/{date}")]
    public IActionResult CalendarHtmlSpecificDate(int displayNumber, DateTime date, [FromQuery] bool preview_colors = false)
    {
        var display = _displayService.GetDisplayById(displayNumber);
        if (display == null || display.Theme == null)
        {
            return NotFound();
        }

        var viewModel = _pageGeneratorService.PageViewModelForDate(display, date, preview_colors);

        return View($"~/Views/CalendarThemes/{display.Theme.FileName}.cshtml", viewModel);
    }

    // GET /config_ui/{display_number}
    [HttpGet("/config_ui/{displayNumber:int}")]
    public async Task<IActionResult> ConfigUiShow(int displayNumber)
    {
        var display = _displayService.GetDisplayById(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        ViewData["NavLink"] = "config_ui";
        ViewData["Title"] = $"Configuration - {display.Name}";
        ViewData["Themes"] = await _themeService.GetActiveThemesAsync();
        ViewData["LastVoltage"] = _displayService.GetVoltage(display);
        ViewData["LastVoltageRaw"] = _displayService.GetConfig(display, "_last_voltage_raw");
        ViewBag.Display = display; // for global layout

        // Pass DisplayService and Display to the view through ViewData
        ViewData["DisplayService"] = _displayService;
        ViewData["Display"] = display;

        return View("ConfigUi", display);
    }

    // POST /config_ui/{display_number}
    [HttpPost("/config_ui/{displayNumber:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigUiSave(int displayNumber, [FromForm] IFormCollection form)
    {
        var display = _displayService.GetDisplayById(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        // Save generic config parameters
        foreach (var paramName in ConfigUiParameters)
        {
            if (form.ContainsKey(paramName))
            {
                var value = form[paramName].ToString();
                _displayService.SetConfig(display, paramName, value);
            }
            else if (paramName.StartsWith("web_calendar") || paramName == "googlefit" ||
                     paramName == "metnoweather" || paramName == "openweather" ||
                     paramName == "telegram" || paramName == "mqtt" || paramName == "ota_mode")
            {
                // Checkboxes: if not present, explicitly set to empty/false
                // FIXME is this needed now when we are using hidden inputs for checkboxes to fix this in JS?
                _displayService.SetConfig(display, paramName, "");
            }
        }

        // Update database columns in the 'displays' table
        if (!display.IsDefault())
        {
            if (form.ContainsKey("display_name"))
            {
                display.Name = form["display_name"].ToString();
            }
            if (form.ContainsKey("display_mac"))
            {
                display.Mac = form["display_mac"].ToString().Trim().ToLowerInvariant();
            }
            if (form.ContainsKey("display_rotation") && int.TryParse(form["display_rotation"], out var rotation))
            {
                display.Rotation = rotation;
            }
            if (form.ContainsKey("display_gamma") && double.TryParse(form["display_gamma"], out var gamma))
            {
                display.Gamma = gamma;
            }
            if (form.ContainsKey("display_border_top") && int.TryParse(form["display_border_top"], out var borderTop))
            {
                display.BorderTop = borderTop;
            }
            if (form.ContainsKey("display_border_right") && int.TryParse(form["display_border_right"], out var borderRight))
            {
                display.BorderRight = borderRight;
            }
            if (form.ContainsKey("display_border_bottom") && int.TryParse(form["display_border_bottom"], out var borderBottom))
            {
                display.BorderBottom = borderBottom;
            }
            if (form.ContainsKey("display_border_left") && int.TryParse(form["display_border_left"], out var borderLeft))
            {
                display.BorderLeft = borderLeft;
            }

            _context.Update(display);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Configuration saved for display {DisplayId}", displayNumber);

        _displayService.EnqueueImageRegenerationRequest(display);

        TempData["Message"] = "Parameters saved.";
        return RedirectToAction(nameof(ConfigUiShow), new { displayNumber });
    }

    // GET /config_ui/theme/{display_number}?theme=portal_with_icons
    [HttpGet("/config_ui/theme/{displayNumber:int}")]
    public async Task<IActionResult> ConfigUiThemeShow(int displayNumber, [FromQuery] int? themeId)
    {
        var display = _displayService.GetDisplayById(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        if (!themeId.HasValue)
        {
            return Content(string.Empty);
        }

        // Get theme metadata from database using ThemeService
        var themeEntity = await _themeService.GetThemeByIdAsync(themeId.Value);

        if (themeEntity == null)
        {
            _logger.LogWarning("Theme ID {ThemeId} not found in database", themeId);
            return Content(string.Empty);
        }

        // Only try to load config if theme has custom config
        if (!themeEntity.HasCustomConfig)
        {
            _logger.LogDebug("Theme [{ThemeName}] has no custom configuration", themeEntity.DisplayName);
            return Content(string.Empty);
        }

        try
        {
            // Try to render the theme configuration partial view
            var viewPath = $"~/Views/CalendarThemes/Configs/{themeEntity.FileName}.cshtml";

            _logger.LogInformation("Rendering theme configuration for theme: {ThemeName}", themeEntity.DisplayName);

            // Pass DisplayService and Display through ViewData for the partial view
            ViewData["DisplayService"] = _displayService;
            ViewData["Display"] = display;

            return PartialView(viewPath, display);
        }
        catch (InvalidOperationException ex)
        {
            // View not found - log as warning since theme says it should have config
            _logger.LogWarning(ex, "Configuration view not found for theme [{ThemeName}] that claims to have custom config", themeEntity.DisplayName);
            return Content(string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering theme [{ThemeName}]", themeEntity.DisplayName);
            return Content(string.Empty);
        }
    }
}