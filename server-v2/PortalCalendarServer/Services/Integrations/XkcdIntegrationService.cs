using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DTOs;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services.Caches;
using System.Text.Json;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Integration service for fetching XKCD comics from the XKCD API.
/// See documentation at https://xkcd.com/json.html
/// </summary>
public class XkcdIntegrationService(
    ILogger<XkcdIntegrationService> logger,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    IDatabaseCacheServiceFactory databaseCacheFactory,
    CalendarContext context,
    Display? display = null) : IntegrationServiceBase(logger, httpClientFactory, memoryCache, databaseCacheFactory, context, display)
{
    public override bool IsConfigured()
    {
        // No configuration needed for XKCD API
        return true;
    }

    /// <summary>
    /// Fetch the latest XKCD comic information
    /// </summary>
    public async Task<XkcdComicData> GetLatestComicAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching latest XKCD comic information");

        // Get JSON from cache or web
        var json = await GetCachedJsonFromWebAsync(cancellationToken);

        // Parse JSON
        var xkcdResponse = JsonSerializer.Deserialize<XkcdApiResponse>(json);
        if (xkcdResponse == null)
        {
            throw new InvalidOperationException("Failed to parse XKCD JSON response");
        }

        // Get image data
        var imageData = await GetCachedImageDataAsync(xkcdResponse.ImageUrl, cancellationToken);

        return new XkcdComicData
        {
            Number = xkcdResponse.Number,
            Title = xkcdResponse.Title,
            Alt = xkcdResponse.Alt,
            ImageUrl = xkcdResponse.ImageUrl,
            ImageData = imageData,
            Year = xkcdResponse.Year,
            Month = xkcdResponse.Month,
            Day = xkcdResponse.Day
        };
    }

    /// <summary>
    /// Fetch JSON from XKCD API with caching (15 minutes)
    /// </summary>
    private async Task<string> GetCachedJsonFromWebAsync(CancellationToken cancellationToken)
    {
        var dbCacheService = databaseCacheFactory.Create(nameof(XkcdIntegrationService), TimeSpan.FromMinutes(15));

        var url = "https://xkcd.com/info.0.json";
        var json = await dbCacheService.GetOrSetAsync(
            async () =>
            {
                logger.LogDebug("XKCD JSON database cache MISS - fetching from web");
                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken);
            },
            new { url = url },
            cancellationToken
        );

        return json;
    }

    /// <summary>
    /// Fetch image data from URL with caching (14 days)
    /// </summary>
    private async Task<byte[]> GetCachedImageDataAsync(string imageUrl, CancellationToken cancellationToken)
    {
        var dbCacheService = databaseCacheFactory.Create(nameof(XkcdIntegrationService), TimeSpan.FromDays(14));

        var imageData = await dbCacheService.GetOrSetAsync(
            async () =>
            {
                logger.LogDebug("XKCD image database cache MISS - fetching from web");
                var response = await httpClient.GetAsync(imageUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            },
            new { url = imageUrl },
            cancellationToken
        );

        return imageData;
    }

    /// <summary>
    /// Determine if image is landscape orientation
    /// An image is considered landscape only if significantly wider (aspect ratio > 4:3)
    /// </summary>
    public static bool DetermineIsLandscape(byte[] imageData)
    {
        try
        {
            using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(imageData);

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
    public static string ConvertToDataUrl(byte[] imageData)
    {
        var base64 = Convert.ToBase64String(imageData);
        return $"data:image/png;base64,{base64}";
    }
}

/// <summary>
/// XKCD comic data retrieved from the API
/// </summary>
public class XkcdComicData
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = [];
    public string Year { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
}

