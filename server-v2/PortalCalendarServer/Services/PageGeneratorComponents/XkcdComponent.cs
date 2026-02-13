using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for displaying XKCD comics on the portal calendar.
/// Uses XkcdIntegrationService for fetching comic data from the web.
/// This component handles the presentation logic for the calendar display.
/// </summary>
public class XkcdComponent : BaseComponent
{
    private readonly XkcdIntegrationService _integrationService;

    public XkcdComponent(
        ILogger<PageGeneratorService> logger,
        DisplayService displayService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        CalendarContext context,
        ILoggerFactory loggerFactory)
        : base(logger, displayService)
    {
        _integrationService = new XkcdIntegrationService(
            loggerFactory.CreateLogger<XkcdIntegrationService>(),
            httpClientFactory,
            memoryCache,
            context,
            null,  // Display is not needed for XKCD integration
            0);
    }

    /// <summary>
    /// Get the XKCD comic information formatted for display
    /// </summary>
    public XkcdInfo GetInfo()
    {
        _logger.LogDebug("Preparing XKCD comic for display");

        // Fetch comic data from integration service
        var comicData = _integrationService.GetLatestComicAsync().GetAwaiter().GetResult();

        // Determine orientation for display
        var isLandscape = XkcdIntegrationService.DetermineIsLandscape(comicData.ImageData);

        // Convert to data URL for embedding in HTML
        var imageAsDataUrl = XkcdIntegrationService.ConvertToDataUrl(comicData.ImageData);
        
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
