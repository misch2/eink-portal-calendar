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
    ILogger<PageGeneratorService> logger,
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
        var imageUrl = displayService.GetConfig(display, "webimage_url");
        var cacheHours = int.Parse(displayService.GetConfig(display, "webimage_cache_hours"));

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
