using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Models;

namespace PortalCalendarServer.Controllers;

[Controller]
public class UiController : Controller
{
    private readonly CalendarContext _context;
    private readonly ILogger<UiController> _logger;

    public UiController(CalendarContext context, ILogger<UiController> logger)
    {
        _context = context;
        _logger = logger;
    }

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
            .OrderBy(d => d.Id)
            .ToListAsync();

        // TODO: Return view with display list
        return Ok(new { message = "Display list page", displays });
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

        // TODO: Return home view
        return Ok(new { message = "Home page", display });
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

        // TODO: Return side-by-side comparison view
        return Ok(new { message = "Test/comparison page", display });
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

        // TODO: Set flash message "Display deleted."
        return RedirectToAction(nameof(SelectDisplay));
    }

    // GET /calendar/{display_number}/html
    [HttpGet("/calendar/{displayNumber:int}/html")]
    public async Task<IActionResult> CalendarHtmlDefaultDate(int displayNumber)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        // TODO: Implement calendar HTML rendering for current date
        var previewColors = Request.Query.ContainsKey("preview_colors") 
            ? bool.Parse(Request.Query["preview_colors"]!) 
            : false;

        return Ok(new { message = "Calendar HTML for current date", display, previewColors });
    }

    // GET /calendar/{display_number}/html/{date}
    [HttpGet("/calendar/{displayNumber:int}/html/{date}")]
    public async Task<IActionResult> CalendarHtmlSpecificDate(int displayNumber, string date)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        if (!DateTime.TryParse(date, out var parsedDate))
        {
            return BadRequest(new { error = "Invalid date format" });
        }

        // TODO: Implement calendar HTML rendering for specific date
        var previewColors = Request.Query.ContainsKey("preview_colors") 
            ? bool.Parse(Request.Query["preview_colors"]!) 
            : false;

        return Ok(new { message = "Calendar HTML for specific date", display, date = parsedDate, previewColors });
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

        // TODO: Get available template names from filesystem
        // TODO: Return configuration UI view
        return Ok(new { message = "Configuration UI page", display });
    }

    // POST /config_ui/{display_number}
    [HttpPost("/config_ui/{displayNumber:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigUiSave(int displayNumber)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        // TODO: Read form parameters and update display configuration
        // TODO: Update database columns in the 'displays' table
        // TODO: Enqueue regenerate_all_images task

        _logger.LogInformation("Configuration saved for display {DisplayId}", displayNumber);

        // TODO: Set flash message "Parameters saved."
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

        if (string.IsNullOrWhiteSpace(theme))
        {
            return Content(string.Empty);
        }

        // Sanitize theme name
        var sanitizedTheme = new string(theme.Where(c => 
            char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ' ').ToArray());

        // TODO: Render theme configuration template
        _logger.LogInformation("Rendering theme configuration for theme: {Theme}", sanitizedTheme);

        return Ok(new { message = "Theme configuration", display, theme = sanitizedTheme });
    }

    // GET /auth/googlefit/{display_number}
    [HttpGet("/auth/googlefit/{displayNumber:int}")]
    public async Task<IActionResult> GoogleFitRedirect(int displayNumber)
    {
        var display = await GetDisplayByIdAsync(displayNumber);
        if (display == null)
        {
            return NotFound();
        }

        // TODO: Build Google OAuth2 URL with proper parameters
        // TODO: Redirect to Google OAuth consent page
        _logger.LogInformation("Redirecting to Google Fit OAuth for display {DisplayId}", displayNumber);

        return Ok(new { message = "Google Fit OAuth redirect", display });
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

        var accessToken = display.GetConfig(_context, "_googlefit_access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { error = "No access token found" });
        }

        // TODO: Return success view with token information
        return Ok(new { message = "Google Fit authentication successful", display, hasToken = true });
    }
}