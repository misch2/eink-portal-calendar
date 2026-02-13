using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models;
using PortalCalendarServer.Models.Weather;
using PortalCalendarServer.Services.Integrations.Weather;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for weather data
/// </summary>
public class WeatherComponent(
    ILogger<PageGeneratorService> logger,
    DisplayService displayService,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    CalendarContext context,
    ILoggerFactory loggerFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly CalendarContext _context = context;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public async Task<WeatherInfo?> GetWeatherAsync(DateTime date)
    {
        // Try Met.no first
        var metNoWeather = await GetMetNoWeatherAsync(date);
        if (metNoWeather != null)
        {
            return metNoWeather;
        }
        // FIXME add fallback to OpenWeatherMap
        return null;
    }

    /// <summary>
    /// Get current weather and forecast from Met.no for the specified date
    /// </summary>
    public async Task<WeatherInfo?> GetMetNoWeatherAsync(DateTime date)
    {
        if (!displayService.GetConfigBool("metnoweather"))
            return null;

        var lat = displayService.GetConfigDouble("lat");
        var lon = displayService.GetConfigDouble("lon");
        var alt = displayService.GetConfigDouble("alt");

        if (lat == null || lon == null || alt == null)
        {
            logger.LogWarning("Met.no weather enabled but lat/lon/alt not configured");
            return null;
        }

        try
        {
            var service = new MetNoWeatherService(
                _loggerFactory.CreateLogger<MetNoWeatherService>(),
                _httpClientFactory,
                _memoryCache,
                _context,
                lat.Value,
                lon.Value,
                alt.Value,
                displayService.CurrentDisplay
            );

            var detailedForecast = await service.GetForecastAsync();
            
            var dtStart = date.ToUniversalTime();
            dtStart = new DateTime(dtStart.Year, dtStart.Month, dtStart.Day, dtStart.Hour, 0, 0, DateTimeKind.Utc);

            // Current weather (1 hour aggregate from now)
            var currentWeather = service.Aggregate(detailedForecast, dtStart, 1);

            // Forecast (multiple aggregated periods)
            var aggregateHours = displayService.GetConfigInt("metnoweather_granularity_hours") ?? 2;
            var forecast = new List<AggregatedWeatherData>();
            
            dtStart = dtStart.AddHours(1);
            for (int i = 0; i < 8; i++)
            {
                var aggregated = service.Aggregate(detailedForecast, dtStart, aggregateHours);
                if (aggregated != null)
                {
                    forecast.Add(aggregated);
                }
                dtStart = dtStart.AddHours(aggregateHours);
            }

            return new WeatherInfo
            {
                CurrentWeather = currentWeather,
                Forecast = forecast
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching Met.no weather data");
            return null;
        }
    }

    /// <summary>
    /// Get current weather and forecast from OpenWeatherMap
    /// </summary>
    public async Task<OpenWeatherInfo?> GetOpenWeatherAsync()
    {
        if (!displayService.GetConfigBool("openweather"))
            return null;

        var apiKey = displayService.GetConfig("openweather_api_key");
        var lat = displayService.GetConfigDouble("lat");
        var lon = displayService.GetConfigDouble("lon");
        var lang = displayService.GetConfig("openweather_lang") ?? "en";

        if (string.IsNullOrEmpty(apiKey) || lat == null || lon == null)
        {
            logger.LogWarning("OpenWeather enabled but API key or lat/lon not configured");
            return null;
        }

        try
        {
            var service = new OpenWeatherService(
                _loggerFactory.CreateLogger<OpenWeatherService>(),
                _httpClientFactory,
                _memoryCache,
                _context,
                apiKey,
                lat.Value,
                lon.Value,
                lang,
                displayService.CurrentDisplay
            );

            var current = await service.FetchCurrentFromWebAsync();
            var forecast = await service.FetchForecastFromWebAsync();

            return new OpenWeatherInfo
            {
                Current = current,
                Forecast = forecast
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching OpenWeather data");
            return null;
        }
    }
}

/// <summary>
/// Weather information from Met.no
/// </summary>
public class WeatherInfo
{
    public AggregatedWeatherData? CurrentWeather { get; set; }
    public List<AggregatedWeatherData> Forecast { get; set; } = [];
}

/// <summary>
/// Weather information from OpenWeatherMap
/// </summary>
public class OpenWeatherInfo
{
    public OpenWeatherCurrent? Current { get; set; }
    public OpenWeatherForecast? Forecast { get; set; }
}
