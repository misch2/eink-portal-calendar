using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;

namespace PortalCalendarServer.Controllers;

[Controller]
public class OtherController : Controller
{
    private readonly CalendarContext _context;
    private readonly ILogger<OtherController> _logger;

    public OtherController(CalendarContext context, ILogger<OtherController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET /auth/googlefit/cb
    // This MUST be first in route matching order (before parameterized routes)
    // Google OAuth restriction: cannot accept additional parameters
    [HttpGet("/auth/googlefit/cb")]
    public async Task<IActionResult> GoogleFitCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogError("Google OAuth error: {Error}", error);
            return BadRequest(new { error = $"Google OAuth error: {error}" });
        }

        if (string.IsNullOrEmpty(code))
        {
            _logger.LogError("No authorization code received from Google");
            return BadRequest(new { error = "No authorization code received" });
        }

        if (string.IsNullOrEmpty(state))
        {
            _logger.LogError("No state parameter received from Google");
            return BadRequest(new { error = "No state parameter received" });
        }

        try
        {
            // TODO: Decode state parameter to get display_number
            // state should be base64 encoded JSON with { display_number: X }
            // TODO: Exchange authorization code for access and refresh tokens
            // TODO: Store tokens in display configuration
            // TODO: Redirect to success page

            _logger.LogInformation("Google Fit OAuth callback received with code");

            return Ok(new { message = "Google Fit OAuth callback", code, state });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google Fit callback");
            return StatusCode(500, new { error = "Error processing OAuth callback" });
        }
    }
}