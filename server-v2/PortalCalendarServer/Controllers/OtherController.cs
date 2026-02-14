using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Services;
using System.Text;
using System.Text.Json;

namespace PortalCalendarServer.Controllers;

[Controller]
public class OtherController : Controller
{
    private readonly CalendarContext _context;
    private readonly ILogger<OtherController> _logger;
    private readonly DisplayService _displayService;

    public OtherController(
        CalendarContext context,
        ILogger<OtherController> logger,
        DisplayService displayService)
    {
        _context = context;
        _logger = logger;
        _displayService = displayService;
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
            return View("~/Views/Ui/AuthError.cshtml", new { Error = error });
        }

        if (string.IsNullOrEmpty(code))
        {
            _logger.LogError("No authorization code received from Google");
            return View("~/Views/Ui/AuthError.cshtml", new { Error = "No authorization code received" });
        }

        if (string.IsNullOrEmpty(state))
        {
            _logger.LogError("No state parameter received from Google");
            return View("~/Views/Ui/AuthError.cshtml", new { Error = "No state parameter received" });
        }

        try
        {
            // Decode state parameter to get display number
            var stateBytes = Convert.FromBase64String(state);
            var stateJson = Encoding.UTF8.GetString(stateBytes);
            var stateData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stateJson);

            if (stateData == null || !stateData.TryGetValue("display_number", out var displayNumberElement))
            {
                _logger.LogError("Invalid state parameter - missing display_number");
                return View("~/Views/Ui/AuthError.cshtml", new { Error = "Invalid state parameter" });
            }

            var displayId = displayNumberElement.GetInt32();
            var display = await _context.Displays
                .Include(d => d.Configs)
                .FirstOrDefaultAsync(d => d.Id == displayId);

            if (display == null)
            {
                _logger.LogError("Display {DisplayId} not found", displayId);
                return View("~/Views/Ui/AuthError.cshtml", new { Error = "Display not found" });
            }

            _displayService.UseDisplay(display);

            // Get OAuth configuration
            var clientId = _displayService.GetConfig("googlefit_client_id");
            var clientSecret = _displayService.GetConfig("googlefit_client_secret");
            var redirectUri = _displayService.GetConfig("googlefit_auth_callback");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
            {
                _logger.LogError("Missing Google Fit OAuth configuration for display {DisplayId}", displayId);
                return View("~/Views/Ui/AuthError.cshtml", new { Error = "Missing OAuth configuration" });
            }

            // Exchange authorization code for tokens
            using var httpClient = new HttpClient();
            var tokenRequest = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            var tokenResponse = await httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(tokenRequest));

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogError("Token exchange failed: {StatusCode} {Content}",
                    tokenResponse.StatusCode, errorContent);
                return View("~/Views/Ui/AuthError.cshtml", new { Error = $"Token exchange failed: {tokenResponse.StatusCode}" });
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokens = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tokenJson);

            if (tokens == null)
            {
                _logger.LogError("Failed to parse token response");
                return View("~/Views/Ui/AuthError.cshtml", new { Error = "Failed to parse token response" });
            }

            // Store tokens
            if (tokens.TryGetValue("access_token", out var accessToken))
            {
                _displayService.SetConfig("_googlefit_access_token", accessToken.GetString() ?? "");
            }

            if (tokens.TryGetValue("refresh_token", out var refreshToken))
            {
                _displayService.SetConfig("_googlefit_refresh_token", refreshToken.GetString() ?? "");
            }

            await _displayService.SaveChangesAsync();

            _logger.LogInformation("Successfully stored Google Fit tokens for display {DisplayId}", displayId);

            return RedirectToAction("GoogleFitSuccess", "Ui", new { display_number = displayId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google Fit callback");
            return View("~/Views/Ui/AuthError.cshtml", new { Error = "Error processing OAuth callback" });
        }
    }
}