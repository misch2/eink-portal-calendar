using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models;
using PortalCalendarServer.Models.DTOs;
using System.Text.Json;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Integration service for fetching XKCD comics from the XKCD API.
/// See documentation at https://xkcd.com/json.html
/// </summary>
public class XkcdIntegrationService : IntegrationServiceBase
{
    public XkcdIntegrationService(
        ILogger<XkcdIntegrationService> logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        CalendarContext context,
        Display? display = null,
        int minimalCacheExpiry = 0)
        : base(logger, httpClientFactory, memoryCache, context, display, minimalCacheExpiry)
    {
    }

    /// <summary>
    /// Fetch the latest XKCD comic information
    /// </summary>
    public async Task<XkcdComicData> GetLatestComicAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Fetching latest XKCD comic information");

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
        var cacheKey = "xkcd:json";
        var cacheExpiration = TimeSpan.FromMinutes(15);

        // Try to get from memory cache first
        if (MemoryCache.TryGetValue<string>(cacheKey, out var cachedJson) && cachedJson != null)
        {
            Logger.LogDebug("XKCD JSON cache HIT");
            return cachedJson;
        }

        Logger.LogDebug("XKCD JSON cache MISS - fetching from web");

        // Fetch from web
        var client = GetHttpClient();
        var url = "https://xkcd.com/info.0.json";

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Cache the result
        MemoryCache.Set(cacheKey, json, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheExpiration,
            Size = json.Length
        });

        return json;
    }

    /// <summary>
    /// Fetch image data from URL with caching (14 days)
    /// </summary>
    private async Task<byte[]> GetCachedImageDataAsync(string imageUrl, CancellationToken cancellationToken)
    {
        var cacheKey = $"xkcd:image:{imageUrl}";
        var cacheExpiration = TimeSpan.FromDays(14);

        // Try to get from memory cache first
        if (MemoryCache.TryGetValue<byte[]>(cacheKey, out var cachedImage) && cachedImage != null)
        {
            Logger.LogDebug("XKCD image cache HIT for {Url}", imageUrl);
            return cachedImage;
        }

        Logger.LogDebug("XKCD image cache MISS - fetching from web: {Url}", imageUrl);

        // Fetch from web
        var client = GetHttpClient();
        var response = await client.GetAsync(imageUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var imageData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        // Cache the result
        MemoryCache.Set(cacheKey, imageData, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheExpiration,
            Size = imageData.Length
        });

        return imageData;
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
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string Year { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
}

