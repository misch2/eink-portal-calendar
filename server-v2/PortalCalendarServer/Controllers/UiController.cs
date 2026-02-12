using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models;
using PortalCalendarServer.Services;

namespace PortalCalendarServer.Controllers;

[Controller]
public class UiController(
    CalendarContext context,
    ILogger<UiController> logger,
    IWebHostEnvironment environment,
    PageGeneratorService pageGeneratorService,
    DisplayService displayService) : Controller
{
    private readonly CalendarContext _context = context;
    private readonly ILogger<UiController> _logger = logger;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly PageGeneratorService _pageGeneratorService = pageGeneratorService;
    private readonly DisplayService _displayService = displayService;

    // Config parameter names that can be saved from the UI
    private static readonly string[] ConfigUiParameters =
    [
        "alive_check_safety_lag_minutes", "alive_check_minimal_failure_count", "alt", "date_culture", "display_title",
        "googlefit", "googlefit_auth_callback", "googlefit_client_id", "googlefit_client_secret",
        "lat", "lon", "max_icons_with_calendar", "max_random_icons", "metnoweather",
        "metnoweather_granularity_hours", "min_random_icons", "mqtt", "mqtt_password", "mqtt_server",
        "mqtt_topic", "mqtt_username", "openweather", "openweather_api_key", "openweather_lang",
        "ota_mode", "telegram", "telegram_api_key", "telegram_chat_id", "theme", "timezone",
        "totally_random_icon", "wakeup_schedule", "web_calendar_ics_url1", "web_calendar_ics_url2",
        "web_calendar_ics_url3", "web_calendar1", "web_calendar2", "web_calendar3"
    ];

    // Helper to get display by ID
    private async Task<Display?> GetDisplayByIdAsync(int displayNumber)
    {
        var display = await _context.Displays
            .Include(d => d.Configs)
            .FirstOrDefaultAsync(d => d.Id == displayNumber);

        if (display == null)
        {
            _logger.LogWarning("Display with ID {DisplayNumber} not found", displayNumber);
        }

        return display;
    }

    // GET /
    [HttpGet("/")]
    public async Task<IActionResult> SelectDisplay()
    {
        var displays = await _context.Displays
            .Include(d => d.Configs)
            .OrderBy(d => d.Id)
            .ToListAsync();

        ViewData["NavLink"] = "index";
        ViewData["Title"] = "Displays";

        return View("DisplayList", displays);
    }

    // GET /home/{display_number}
    [HttpGet("/home/{displayNumber:int}")]
    public async Task<IActionResult> Home(int displayNumber)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
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
    public async Task<IActionResult> Test(int displayNumber)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        ViewData["NavLink"] = "compare";
        ViewData["Title"] = $"Test - {display.Name}";

        return View("Test", display);
    }

    // POST /delete/{display_number}
    [HttpPost("/delete/{displayNumber:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDisplay(int displayNumber)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        _context.Displays.Remove(display);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Display deleted: {DisplayId}", displayNumber);

        TempData["Message"] = "Display deleted.";
        return RedirectToAction(nameof(SelectDisplay));
    }

    // GET /calendar/{display_number}/html
    [HttpGet("/calendar/{displayNumber:int}/html")]
    public async Task<IActionResult> CalendarHtmlDefaultDate(int displayNumber, [FromQuery] bool preview_colors = false)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }
        _displayService.UseDisplay(display);

        var viewModel = _pageGeneratorService.PageViewModelForDate(display, DateTime.UtcNow, preview_colors);

        // TODO: Return calendar theme view
        var theme = _displayService.GetConfig("theme") ?? "default";
        return View($"~/Views/CalendarThemes/{theme}.cshtml", viewModel);
    }

    // GET /calendar/{display_number}/html/{date}
    [HttpGet("/calendar/{displayNumber:int}/html/{date}")]
    public async Task<IActionResult> CalendarHtmlSpecificDate(int displayNumber, string date, [FromQuery] bool preview_colors = false)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }
        _displayService.UseDisplay(display);

        if (!DateTime.TryParse(date, out var parsedDate))
        {
            return BadRequest(new { error = "Invalid date format" });
        }

        var viewModel = _pageGeneratorService.PageViewModelForDate(display, parsedDate, preview_colors);

        // TODO: Return calendar theme view
        var theme = _displayService.GetConfig("theme") ?? "default";
        return View($"~/Views/CalendarThemes/{theme}.cshtml", viewModel);
    }

    // GET /config_ui/{display_number}
    [HttpGet("/config_ui/{displayNumber:int}")]
    public async Task<IActionResult> ConfigUiShow(int displayNumber)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }
        _displayService.UseDisplay(display);

        // Get available template names from filesystem
        var themesPath = Path.Combine(_environment.ContentRootPath, "Views", "CalendarThemes");
        var templateNames = new List<string>();

        if (Directory.Exists(themesPath))
        {
            templateNames = Directory.GetFiles(themesPath, "*.cshtml")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => !n.StartsWith("_"))
                .ToList();
        }

        ViewData["NavLink"] = "config_ui";
        ViewData["Title"] = $"Configuration - {display.Name}";
        ViewData["TemplateNames"] = templateNames;
        ViewData["CurrentTheme"] = _displayService.GetConfig("theme");
        ViewData["LastVoltage"] = display.Voltage();
        ViewData["LastVoltageRaw"] = _displayService.GetConfig("_last_voltage_raw");

        // Pass DisplayService to the view through ViewData
        ViewData["DisplayService"] = _displayService;

        return View("ConfigUi", display);
    }

    // POST /config_ui/{display_number}
    [HttpPost("/config_ui/{displayNumber:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigUiSave(int displayNumber, [FromForm] IFormCollection form)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }
        _displayService.UseDisplay(display);

        // Save generic config parameters
        foreach (var paramName in ConfigUiParameters)
        {
            if (form.ContainsKey(paramName))
            {
                var value = form[paramName].ToString();
                _displayService.SetConfig(paramName, value);
            }
            else if (paramName.StartsWith("web_calendar") || paramName == "googlefit" ||
                     paramName == "metnoweather" || paramName == "openweather" ||
                     paramName == "telegram" || paramName == "mqtt" || paramName == "ota_mode")
            {
                // Checkboxes: if not present, set to empty/false
                _displayService.SetConfig(paramName, "");
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

        await _displayService.SaveChangesAsync();

        _logger.LogInformation("Configuration saved for display {DisplayId}", displayNumber);

        // TODO: Enqueue regenerate_all_images task

        TempData["Message"] = "Parameters saved.";
        return RedirectToAction(nameof(ConfigUiShow), new { displayNumber });
    }

    // GET /config_ui/theme/{display_number}?theme=portal_with_icons
    [HttpGet("/config_ui/theme/{displayNumber:int}")]
    public async Task<IActionResult> ConfigUiThemeShow(int displayNumber, [FromQuery] string? theme)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }
        _displayService.UseDisplay(display);

        if (string.IsNullOrWhiteSpace(theme))
        {
            return Content(string.Empty);
        }

        // Sanitize theme name
        var sanitizedTheme = new string(theme.Where(c =>
            char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ' ').ToArray());

        try
        {
            // Try to render the theme configuration partial view
            var viewPath = $"~/Views/CalendarThemes/Configs/{sanitizedTheme}.cshtml";

            _logger.LogInformation("Rendering theme configuration for theme: {Theme}", sanitizedTheme);

            return PartialView(viewPath, display);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering theme [{Theme}]", sanitizedTheme);
            return Content(string.Empty);
        }
    }

    // GET /auth/googlefit/{display_number}
    [HttpGet("/auth/googlefit/{displayNumber:int}")]
    public async Task<IActionResult> GoogleFitRedirect(int displayNumber)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return BadRequest(new { error = "Display not found" });
        }
        _displayService.UseDisplay(display);

        // Build Google OAuth2 URL
        var clientId = _displayService.GetConfig("googlefit_client_id");
        var callback = _displayService.GetConfig("googlefit_auth_callback");
        var scope = "https://www.googleapis.com/auth/fitness.body.read";

        if (clientId == null || callback == null)
        {
            return BadRequest(new { error = "Google Fit client ID or callback URL not configured" });
        }

        var state = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                System.Text.Json.JsonSerializer.Serialize(new { display_number = displayNumber })
            )
        );

        var url = "https://accounts.google.com/o/oauth2/v2/auth" +
                  $"?client_id={Uri.EscapeDataString(clientId)}" +
                  "&access_type=offline&prompt=consent" +
                  "&response_type=code" +
                  $"&scope={Uri.EscapeDataString(scope)}" +
                  $"&state={Uri.EscapeDataString(state)}" +
                  "&include_granted_scopes=true" +
                  $"&redirect_uri={Uri.EscapeDataString(callback)}";

        _logger.LogInformation("Redirecting to Google Fit OAuth for display {DisplayId}", displayNumber);

        return Redirect(url);
    }

    // GET /auth/googlefit/success/{display_number}
    [HttpGet("/auth/googlefit/success/{displayNumber:int}")]
    public async Task<IActionResult> GoogleFitSuccess(int displayNumber)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }
        _displayService.UseDisplay(display);

        var accessToken = _displayService.GetConfig("_googlefit_access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { error = "No access token found" });
        }

        ViewData["NavLink"] = "config_ui";
        ViewData["Title"] = "Google Fit Authentication Successful";
        ViewData["AccessToken"] = accessToken;
        ViewData["RefreshToken"] = _displayService.GetConfig("_googlefit_refresh_token");

        return View("AuthSuccess", display);
    }
}