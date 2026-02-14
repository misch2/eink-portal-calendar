using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortalCalendarServer.Services.Integrations.Weather;

/// <summary>
/// Integration service for OpenWeather API.
/// Based on PortalCalendar::Integration::Weather::OpenWeather from Perl.
/// </summary>
public class OpenWeatherService : IntegrationServiceBase
{
    private readonly string _apiKey;
    private readonly double _latitude;
    private readonly double _longitude;
    private readonly string _language;

    public OpenWeatherService(
        ILogger<OpenWeatherService> logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        CalendarContext context,
        string apiKey,
        double latitude,
        double longitude,
        string language = "en",
        Display? display = null,
        int minimalCacheExpiry = 0)
        : base(logger, httpClientFactory, memoryCache, context, display, minimalCacheExpiry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _apiKey = apiKey;
        _latitude = latitude;
        _longitude = longitude;
        _language = language;
    }

    /// <summary>
    /// HTTP cache age: 30 minutes (matching Perl implementation)
    /// </summary>
    protected override int HttpMaxCacheAge => 30 * 60;

    /// <summary>
    /// Fetch current weather from OpenWeather API
    /// </summary>
    public async Task<OpenWeatherCurrent?> FetchCurrentFromWebAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://api.openweathermap.org/data/2.5/weather" +
                  $"?lat={_latitude:F3}" +
                  $"&lon={_longitude:F3}" +
                  $"&units=metric" +
                  $"&appid={_apiKey}" +
                  $"&lang={_language}";

        var cacheKey = $"openweather:current:{url}";

        // Try memory cache first
        if (MemoryCache.TryGetValue<OpenWeatherCurrent>(cacheKey, out var cached) && cached != null)
        {
            Logger.LogDebug("OpenWeather current cache HIT");
            return cached;
        }

        Logger.LogDebug("OpenWeather current cache MISS, fetching from API");

        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<OpenWeatherCurrent>(json);

        if (data != null)
        {
            // Cache for 30 minutes
            MemoryCache.Set(cacheKey, data, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                Size = 1024
            });
        }

        return data;
    }

    /// <summary>
    /// Fetch weather forecast from OpenWeather API
    /// </summary>
    public async Task<OpenWeatherForecast?> FetchForecastFromWebAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://api.openweathermap.org/data/2.5/forecast" +
                  $"?lat={_latitude}" +
                  $"&lon={_longitude}" +
                  $"&units=metric" +
                  $"&appid={_apiKey}" +
                  $"&lang={_language}";

        var cacheKey = $"openweather:forecast:{url}";

        // Try memory cache first
        if (MemoryCache.TryGetValue<OpenWeatherForecast>(cacheKey, out var cached) && cached != null)
        {
            Logger.LogDebug("OpenWeather forecast cache HIT");
            return cached;
        }

        Logger.LogDebug("OpenWeather forecast cache MISS, fetching from API");

        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<OpenWeatherForecast>(json);

        if (data != null)
        {
            // Cache for 30 minutes
            MemoryCache.Set(cacheKey, data, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                Size = 2048
            });
        }

        return data;
    }
}

// JSON response models for OpenWeather API
public class OpenWeatherCurrent
{
    [JsonPropertyName("coord")]
    public OpenWeatherCoord? Coord { get; set; }

    [JsonPropertyName("weather")]
    public List<OpenWeatherWeatherItem>? Weather { get; set; }

    [JsonPropertyName("main")]
    public OpenWeatherMain? Main { get; set; }

    [JsonPropertyName("wind")]
    public OpenWeatherWind? Wind { get; set; }

    [JsonPropertyName("clouds")]
    public OpenWeatherClouds? Clouds { get; set; }

    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class OpenWeatherForecast
{
    [JsonPropertyName("list")]
    public List<OpenWeatherForecastItem>? List { get; set; }

    [JsonPropertyName("city")]
    public OpenWeatherCity? City { get; set; }
}

public class OpenWeatherForecastItem
{
    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("main")]
    public OpenWeatherMain? Main { get; set; }

    [JsonPropertyName("weather")]
    public List<OpenWeatherWeatherItem>? Weather { get; set; }

    [JsonPropertyName("clouds")]
    public OpenWeatherClouds? Clouds { get; set; }

    [JsonPropertyName("wind")]
    public OpenWeatherWind? Wind { get; set; }

    [JsonPropertyName("pop")]
    public double Pop { get; set; }

    [JsonPropertyName("rain")]
    public OpenWeatherPrecipitation? Rain { get; set; }

    [JsonPropertyName("snow")]
    public OpenWeatherPrecipitation? Snow { get; set; }
}

public class OpenWeatherCoord
{
    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }
}

public class OpenWeatherWeatherItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("main")]
    public string? Main { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}

public class OpenWeatherMain
{
    [JsonPropertyName("temp")]
    public double Temp { get; set; }

    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; set; }

    [JsonPropertyName("temp_min")]
    public double TempMin { get; set; }

    [JsonPropertyName("temp_max")]
    public double TempMax { get; set; }

    [JsonPropertyName("pressure")]
    public double Pressure { get; set; }

    [JsonPropertyName("humidity")]
    public double Humidity { get; set; }
}

public class OpenWeatherWind
{
    [JsonPropertyName("speed")]
    public double Speed { get; set; }

    [JsonPropertyName("deg")]
    public double Deg { get; set; }
}

public class OpenWeatherClouds
{
    [JsonPropertyName("all")]
    public double All { get; set; }
}

public class OpenWeatherPrecipitation
{
    [JsonPropertyName("3h")]
    public double ThreeHour { get; set; }
}

public class OpenWeatherCity
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("coord")]
    public OpenWeatherCoord? Coord { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}
