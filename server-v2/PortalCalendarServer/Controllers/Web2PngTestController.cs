using Microsoft.AspNetCore.Mvc;
using PortalCalendarServer.Services;

namespace PortalCalendarServer.Controllers;

/// <summary>
/// Example controller demonstrating Web2PngService usage
/// </summary>
[ApiController]
[Route("api/web2png")]
public class Web2PngTestController : ControllerBase
{
    private readonly Web2PngService _web2PngService;
    private readonly ILogger<Web2PngTestController> _logger;
    private readonly IWebHostEnvironment _environment;

    public Web2PngTestController(
        Web2PngService web2PngService, 
        ILogger<Web2PngTestController> logger,
        IWebHostEnvironment environment)
    {
        _web2PngService = web2PngService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Test endpoint to convert a URL to PNG
    /// </summary>
    /// <param name="url">URL to capture (e.g., https://example.com)</param>
    /// <param name="width">Screenshot width (default: 800)</param>
    /// <param name="height">Screenshot height (default: 480)</param>
    /// <param name="delay">Delay in milliseconds for fonts to load (default: 2000)</param>
    /// <returns>PNG file</returns>
    [HttpGet("test")]
    [Tags("Web2Png", "Testing")]
    public async Task<IActionResult> TestConversion(
        [FromQuery] string url = "https://example.com",
        [FromQuery] int width = 800,
        [FromQuery] int height = 480,
        [FromQuery] int delay = 2000)
    {
        try
        {
            // Create a temporary output path
            var outputDir = Path.Combine(_environment.ContentRootPath, "generated_images");
            Directory.CreateDirectory(outputDir);
            
            var fileName = $"test_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
            var outputPath = Path.Combine(outputDir, fileName);

            _logger.LogInformation("Converting URL {Url} to PNG at {OutputPath}", url, outputPath);

            // Convert URL to PNG
            await _web2PngService.ConvertUrlAsync(
                url,
                width,
                height,
                outputPath,
                delay,
                HttpContext.RequestAborted);

            // Return the generated PNG file
            var fileBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
            return File(fileBytes, "image/png", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert URL to PNG");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Example of how to use Web2PngService in a background task
    /// (This is how it should be used for display regeneration)
    /// </summary>
    [HttpPost("regenerate/{displayId}")]
    [Tags("Web2Png", "Testing")]
    public async Task<IActionResult> RegenerateDisplayImage(int displayId)
    {
        try
        {
            // In real implementation, you would:
            // 1. Get the display from database
            // 2. Build the URL for the display's HTML view
            // 3. Call Web2PngService to generate the PNG
            
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var url = $"{baseUrl}/calendar/{displayId}/html";
            
            var outputDir = Path.Combine(_environment.ContentRootPath, "generated_images");
            Directory.CreateDirectory(outputDir);
            
            var outputPath = Path.Combine(outputDir, $"current_calendar_{displayId}.png");

            _logger.LogInformation("Regenerating image for display {DisplayId} from {Url}", displayId, url);

            // This would typically be in a background job/task
            await _web2PngService.ConvertUrlAsync(
                url,
                800,  // These should come from display.Width
                480,  // These should come from display.Height
                outputPath,
                delayMs: 2000,
                HttpContext.RequestAborted);

            return Ok(new 
            { 
                message = "Image regenerated successfully",
                displayId,
                outputPath,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate display image");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
