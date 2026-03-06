using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// 
/// </summary>
public class WebImageComponent(
    IDisplayService displayService,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    IDatabaseCacheServiceFactory databaseCacheFactory,
    CalendarContext context,
    ILoggerFactory loggerFactory
    )
{

    public WebImageInfo GetDetails(Display display)
    {
        var imageUrl = displayService.GetConfig(display, "webimage_url")
            ?? throw new InvalidOperationException("webimage_url configuration is not set for this display");
        var cacheHoursStr = displayService.GetConfig(display, "webimage_cache_hours")
            ?? throw new InvalidOperationException("webimage_cache_hours configuration is not set for this display");
        var cacheHours = int.Parse(cacheHoursStr);

        var integrationService = new WebImageIntegrationService(
           loggerFactory.CreateLogger<WebImageIntegrationService>(),
           httpClientFactory,
           memoryCache,
           databaseCacheFactory,
           context,
           imageUrl,
           cacheHours
           );

        var imageContent = integrationService.GetCachedImageDataAsync(CancellationToken.None).GetAwaiter().GetResult();
        var ret = new WebImageInfo
        {
            ImageAsDataUrl = integrationService.ConvertToDataUrl(imageContent)
        };
        return ret;
    }
};

public class WebImageInfo
{
    // For now, no additional info is needed, but this can be extended in the future if needed
    public required string ImageAsDataUrl;
}
