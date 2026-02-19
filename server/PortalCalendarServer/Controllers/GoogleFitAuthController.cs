using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Fitness.v1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Services;
using System.Text;
using System.Text.Json;

namespace PortalCalendarServer.Controllers;

[Controller]
public class GoogleFitAuthController : Controller
{
    private readonly CalendarContext _context;
    private readonly ILogger<GoogleFitAuthController> _logger;
    private readonly IDisplayService _displayService;

    public GoogleFitAuthController(
        CalendarContext context,
        ILogger<GoogleFitAuthController> logger,
        IDisplayService displayService)
    {
        _context = context;
        _logger = logger;
        _displayService = displayService;
    }

    /// <summary>
    /// Redirects user to a GoogleFit consent screen for a specific display.
    /// </summary>
    /// <param name="displayNumber"></param>
    /// <returns></returns>

    // GET /auth/googlefit/{display_number}
    [HttpGet("/auth/googlefit/{displayNumber:int}")]
    public IActionResult GoogleFitRedirect(int displayNumber)
    {
        var display = _displayService.GetDisplayById(displayNumber);
        if (display == null)
        {
            return BadRequest(new { error = "Display not found" });
        }

        var clientId = _displayService.GetConfig(display, "googlefit_client_id");
        var clientSecret = _displayService.GetConfig(display, "googlefit_client_secret");

        // Redirect URI must be generated from the current external host and port, i.e. from Host etc. header
        var redirectUri = Url.Action(nameof(GoogleFitCallback), "GoogleFitAuth", null, Request.Scheme, Request.Host.Value);

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return BadRequest(new { error = "Google Fit OAuth configuration incomplete" });
        }

        // Encode state parameter
        var state = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { display_number = displayNumber })
            )
        );

        // Build authorization URL using Google.Apis.Auth
        var codeRequestUrl = new AuthorizationCodeRequestUrl(new Uri(GoogleAuthConsts.AuthorizationUrl))
        {
            ClientId = clientId,
            RedirectUri = redirectUri,
            Scope = FitnessService.Scope.FitnessBodyRead,
            State = state,
            ResponseType = "code"
        };

        var baseUrl = codeRequestUrl.Build();

        // Add additional parameters using UriBuilder
        var uriBuilder = new UriBuilder(baseUrl);
        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        query["access_type"] = "offline"; // Get refresh token
        query["prompt"] = "consent"; // Force consent to ensure refresh token
        query["include_granted_scopes"] = "true";
        uriBuilder.Query = query.ToString();

        var authorizationUrl = uriBuilder.Uri.ToString();

        _logger.LogInformation("Redirecting to Google Fit OAuth for display {DisplayId}", displayNumber);

        return Redirect(authorizationUrl);
    }

    // GET /auth/googlefit/cb
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
            return View("AuthError", new { Error = error });
        }

        if (string.IsNullOrEmpty(code))
        {
            _logger.LogError("No authorization code received from Google");
            return View("AuthError", new { Error = "No authorization code received" });
        }

        if (string.IsNullOrEmpty(state))
        {
            _logger.LogError("No state parameter received from Google");
            return View("AuthError", new { Error = "No state parameter received" });
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
                return View("AuthError", new { Error = "Invalid state parameter" });
            }

            var displayId = displayNumberElement.GetInt32();
            var display = await _context.Displays.FirstOrDefaultAsync(d => d.Id == displayId);

            if (display == null)
            {
                _logger.LogError("Display {DisplayId} not found", displayId);
                return View("AuthError", new { Error = "Display not found" });
            }

            // Get OAuth configuration
            var clientId = _displayService.GetConfig(display, "googlefit_client_id");
            var clientSecret = _displayService.GetConfig(display, "googlefit_client_secret");

            // Redirect URI must be generated from the current external host and port, i.e. from Host etc. header
            var redirectUri = Url.Action(nameof(GoogleFitCallback), "GoogleFitAuth", null, Request.Scheme, Request.Host.Value);

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                _logger.LogError("Missing Google Fit OAuth configuration for display {DisplayId}", displayId);
                return View("AuthError", new { Error = "Missing OAuth configuration" });
            }

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { FitnessService.Scope.FitnessBodyRead }
            });

            var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                "user",
                code,
                redirectUri,
                CancellationToken.None);

            _displayService.SetConfig(display, "_googlefit_access_token", tokenResponse.AccessToken);
            _displayService.SetConfig(display, "_googlefit_refresh_token", tokenResponse.RefreshToken);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully stored Google Fit tokens for display {DisplayId}", displayId);

            return RedirectToAction(nameof(AuthSuccess), new { displayNumber = displayId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google Fit callback");
            return View("AuthError", new { Error = "Error processing OAuth callback" });
        }
    }

    // GET /auth/googlefit/success/{displayNumber}
    [HttpGet("/auth/googlefit/success/{displayNumber:int}")]
    public IActionResult AuthSuccess(int displayNumber)
    {
        var display = _displayService.GetDisplayById(displayNumber);
        if (display == null)
        {
            _logger.LogError("Display {DisplayId} not found", displayNumber);
            return NotFound();
        }

        var accessToken = _displayService.GetConfig(display, "_googlefit_access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogError("No access token found for display {DisplayId}", displayNumber);
            return BadRequest(new { error = "No access token found" });
        }

        ViewData["NavLink"] = "config_ui";
        ViewData["Title"] = "Google Fit Authentication Successful";
        ViewData["AccessToken"] = accessToken;
        ViewData["RefreshToken"] = _displayService.GetConfig(display, "_googlefit_refresh_token");
        ViewBag.Display = display;

        return View(display);
    }
}