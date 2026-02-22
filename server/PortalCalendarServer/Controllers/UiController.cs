using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Controllers.Filters;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Models.POCOs;
using PortalCalendarServer.Services;

namespace PortalCalendarServer.Controllers;

[Controller]
[Authorize]
public class UiController(
    CalendarContext context,
    ILogger<UiController> logger,
    IWebHostEnvironment environment,
    PageGeneratorService pageGeneratorService,
    IDisplayService displayService,
    ThemeService themeService
    ) : Controller, IAsyncResultFilter
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
        "googlefit", "googlefit_client_id", "googlefit_client_secret",
        "lat", "lon", "max_icons_with_calendar", "max_random_icons", "metnoweather",
        "metnoweather_granularity_hours", "min_random_icons", "mqtt", "mqtt_password", "mqtt_server",
        "mqtt_topic", "mqtt_username", "openweather", "openweather_api_key", "openweather_lang",
        "ota_mode", "telegram", "telegram_api_key", "telegram_chat_id", "timezone",
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
    [HttpGet("/calendar/{displayNumber:int}/html", Name = Constants.CalendarHtmlDefaultDate)]
    [Authorize("CookiesOrInternalToken")]
    [DisplayRenderErrorHandling]
    public IActionResult CalendarHtmlDefaultDate(int displayNumber, [FromQuery] bool preview_colors = false, [FromQuery] string? force_error = null)
    {
        return CalendarHtmlSpecificDate(displayNumber, DateTime.UtcNow, preview_colors, force_error);
    }

    // GET /calendar/{display_number}/html/{date}
    [HttpGet("/calendar/{displayNumber:int}/html/{date}")]
    [Authorize("CookiesOrInternalToken")]
    [DisplayRenderErrorHandling]
    public IActionResult CalendarHtmlSpecificDate(int displayNumber, DateTime date, [FromQuery] bool preview_colors = false, [FromQuery] string? force_error = null)
    {
        var display = _displayService.GetDisplayById(displayNumber);
        if (display == null)
        {
            return NotFound("Display with this ID not found");
        }

        if (!string.IsNullOrEmpty(force_error))
        {
            _logger.LogWarning("Forcing error page for display {DisplayId}: {ErrorMessage}", displayNumber, force_error);
            return CalendarErrorView(new InvalidOperationException(force_error), display);
        }

        if (display.Theme == null)
        {
            return UnprocessableEntity("Display has no theme assigned");
        }

        try
        {
            var viewModel = _pageGeneratorService.PageViewModelForDate(display, date, preview_colors);
            return View($"~/Views/CalendarThemes/{display.Theme.FileName}.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            // Catch errors during view model preparation (before Razor rendering).
            // Errors during .cshtml rendering are caught by OnResultExecutionAsync.
            _logger.LogError(ex, "Error preparing calendar view model for display {DisplayId}, theme: {ThemeFileName}",
                displayNumber, display.Theme.FileName);

            return CalendarErrorView(ex, display);
        }
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
                // Checkboxes "Integration Enabled" for each integration: if not present, explicitly set to empty/false because they are disabled by default in the UI
                _displayService.SetConfig(display, paramName, "");
            }
        }

        // Update database columns in the 'displays' table
        if (!display.IsDefault())
        {
            if (form.ContainsKey("display_name"))
            {
                display.Name = form["display_name"].ToString();
                var nameClash = await _context.Displays
                    .AnyAsync(d => d.Id != display.Id && d.Name == display.Name);
                if (nameClash)
                {
                    TempData["Error"] = $"Display name '{display.Name}' is already used by another display.";
                    return RedirectToAction(nameof(ConfigUiShow), new { displayNumber });
                }

            }
            if (form.ContainsKey("display_mac"))
            {
                display.Mac = form["display_mac"].ToString().Trim().ToLowerInvariant();
                var macClash = await _context.Displays
                    .AnyAsync(d => d.Id != display.Id && d.Mac == display.Mac);
                if (macClash)
                {
                    TempData["Error"] = $"MAC address '{display.Mac}' is already used by another display.";
                    return RedirectToAction(nameof(ConfigUiShow), new { displayNumber });
                }

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
            if (form.ContainsKey("theme_id") && int.TryParse(form["theme_id"], out var theme_id))
            {
                display.ThemeId = theme_id;
            }
            if (form.ContainsKey("color_variant"))
            {
                var code = form["color_variant"].ToString();
                if (string.IsNullOrEmpty(code))
                {
                    display.ColorVariantCode = null;
                }
                else
                {
                    display.ColorVariantCode = form["color_variant"].ToString();
                }
            }
            if (form.ContainsKey("display_type"))
            {
                var code = form["display_type"].ToString();
                if (string.IsNullOrEmpty(code))
                {
                    display.DisplayTypeCode = null;
                }
                else
                {
                    display.DisplayTypeCode = form["display_type"].ToString();
                }
            }
            if (form.ContainsKey("dithering_type"))
            {
                var code = form["dithering_type"].ToString();
                if (string.IsNullOrEmpty(code))
                {
                    display.DitheringTypeCode = null;
                }
                else
                {
                    display.DitheringTypeCode = form["dithering_type"].ToString();
                }
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

    /// <summary>
    /// Returns the error theme view for a failed calendar rendering.
    /// Used both from action methods (for view model preparation errors)
    /// and from the result filter (for .cshtml rendering errors).
    /// </summary>
    private ViewResult CalendarErrorView(Exception ex, Display? display)
    {
        Dictionary<string, string>? cssColors = null;
        try
        {
            cssColors = display?.CssColorMap(false);
        }
        catch
        {
            // Ignore - we're already handling an error
        }

        var errorModel = new ErrorViewModel
        {
            Message = ex.Message,
            Details = ex.ToString(),
            ShowDetails = _environment.IsDevelopment(),
            CssColors = cssColors
        };

        return View("~/Views/CalendarThemes/_Error.cshtml", errorModel);
    }

    /// <summary>
    /// Result filter that catches exceptions thrown during Razor view rendering (.cshtml execution).
    /// View rendering happens AFTER the action method returns, so try/catch in action methods
    /// and OnActionExecuted cannot catch .cshtml errors.
    /// </summary>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var isCalendarAction = context.ActionDescriptor.EndpointMetadata
            .OfType<DisplayRenderErrorHandlingAttribute>()
            .Any();

        if (!isCalendarAction)
        {
            await next();
            return;
        }

        // Buffer the response so we can replace it entirely on error.
        // Without this, Razor may have already flushed partial HTML to the client.
        var originalBody = context.HttpContext.Response.Body;
        using var bufferedBody = new MemoryStream();
        context.HttpContext.Response.Body = bufferedBody;

        var resultContext = await next();

        if (resultContext.Exception != null && !resultContext.ExceptionHandled)
        {
            _logger.LogError(resultContext.Exception,
                "Error rendering calendar view for action: {ActionName}",
                context.ActionDescriptor.DisplayName);

            // Reset the buffered stream and render the error theme instead
            bufferedBody.SetLength(0);
            context.HttpContext.Response.StatusCode = 200;
            context.HttpContext.Response.ContentType = "text/html";
            context.HttpContext.Response.Headers.ContentLength = null;

            var errorViewResult = CalendarErrorView(resultContext.Exception, null);
            await errorViewResult.ExecuteResultAsync(context);

            resultContext.ExceptionHandled = true;
        }

        // Copy the buffered response to the real stream
        bufferedBody.Position = 0;
        await bufferedBody.CopyToAsync(originalBody);
        context.HttpContext.Response.Body = originalBody;
    }
}