using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>

/// </summary>
public class WebImageIntegrationService(
    ILogger<WebImageIntegrationService> loggerParam,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    IDatabaseCacheServiceFactory databaseCacheFactory,
    CalendarContext context,
    string imageUrl,
    int cacheHours
    ) : IntegrationServiceBase(loggerParam, httpClientFactory, memoryCache, databaseCacheFactory, context)
{
    private new readonly ILogger<WebImageIntegrationService> logger = loggerParam;
    private readonly string _imageUrl = imageUrl;
    private readonly int _cacheHours = cacheHours;

    public override bool IsConfigured(Display display)
    {
        return string.IsNullOrWhiteSpace(_imageUrl) == false;
    }

    /// <summary>
    /// Fetch image data from URL with caching (14 days)
    /// </summary>
    public async Task<byte[]> GetCachedImageDataAsync(CancellationToken cancellationToken)
    {
        // FIXME make the time configurable too!
        var dbCacheService = databaseCacheFactory.Create(nameof(WebImageIntegrationService), TimeSpan.FromHours(_cacheHours));

        var imageData = await dbCacheService.GetOrSetAsync(
            async () =>
            {
                logger.LogDebug("Image database cache MISS - fetching from web");
                var response = await httpClient.GetAsync(_imageUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            },
            new { url = _imageUrl },
            cancellationToken
        );

        return imageData;
    }

    /// <summary>
    /// Convert image data to data URL (base64 encoded)
    /// </summary>
    public string ConvertToDataUrl(byte[] imageData)
    {
        var base64 = Convert.ToBase64String(imageData);
        return $"data:image/png;base64,{base64}";
    }
}
