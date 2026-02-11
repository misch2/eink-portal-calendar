using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Services.Integrations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for displaying XKCD comics on the portal calendar.
/// Uses XkcdIntegrationService for fetching comic data from the web.
/// This component handles the presentation logic for the calendar display.
/// </summary>
public class XkcdComponent : BaseComponent
{
    private readonly XkcdIntegrationService _integrationService;
    private XkcdInfo? _xkcdInfo;

    public XkcdComponent(
        ILogger<PageGeneratorService> logger,
        DisplayService? displayService,
        DateTime date,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        CalendarContext context)
        : base(logger, displayService, date)
    {
        // Create logger for XkcdIntegrationService
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });

        _integrationService = new XkcdIntegrationService(
            loggerFactory.CreateLogger<XkcdIntegrationService>(),
            httpClientFactory,
            memoryCache,
            context,
            null,  // Display is not needed for XKCD integration
            0);
    }

    /// <summary>
    /// Get the XKCD comic information formatted for display (lazy loaded)
    /// </summary>
    public XkcdInfo Info
    {
        get
        {
            if (_xkcdInfo == null)
            {
                _xkcdInfo = GetXkcdInfoForDisplay();
            }
            return _xkcdInfo;
        }
    }

    /// <summary>
    /// Fetch XKCD comic and prepare it for display
    /// </summary>
    private XkcdInfo GetXkcdInfoForDisplay()
    {
        _logger.LogDebug("Preparing XKCD comic for display");

        // Fetch comic data from integration service
        var comicData = _integrationService.GetLatestComicAsync().GetAwaiter().GetResult();

        // Determine orientation for display
        var isLandscape = DetermineIsLandscape(comicData.ImageData);

        // Convert to data URL for embedding in HTML
        var imageAsDataUrl = ConvertToDataUrl(comicData.ImageData);
        return new XkcdInfo
        {
            Title = comicData.Title,
            Alt = comicData.Alt,
            ImageUrl = comicData.ImageUrl,
            ImageAsDataUrl = imageAsDataUrl,
            ImageIsLandscape = isLandscape,
            Number = comicData.Number,
            Year = comicData.Year,
            Month = comicData.Month,
            Day = comicData.Day
        };
    }

    /// <summary>
    /// Determine if image is landscape orientation
    /// An image is considered landscape only if significantly wider (aspect ratio > 4:3)
    /// </summary>
    private bool DetermineIsLandscape(byte[] imageData)
    {
        try
        {
            using var image = Image.Load<Rgba32>(imageData);

            if (image.Width == 0 || image.Height == 0)
            {
                return false;
            }

            // Only significantly wider images are considered landscape
            var aspectRatio = (double)image.Width / image.Height;
            return aspectRatio > (4.0 / 3.0);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Convert image data to data URL (base64 encoded)
    /// </summary>
    private string ConvertToDataUrl(byte[] imageData)
    {
        var base64 = Convert.ToBase64String(imageData);
        return $"data:image/png;base64,{base64}";
    }

}

/// <summary>
/// XKCD comic information formatted for display in the calendar
/// </summary>
public class XkcdInfo
{
    public string Title { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAsDataUrl { get; set; } = string.Empty;
    public bool ImageIsLandscape { get; set; }
    public int Number { get; set; }
    public string Year { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
}
