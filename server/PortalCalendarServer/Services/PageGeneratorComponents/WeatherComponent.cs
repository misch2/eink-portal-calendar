using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Models.Weather;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Services.Integrations.Weather;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for weather data
/// </summary>
public class WeatherComponent(
    ILogger<PageGeneratorService> logger,
    IDisplayService displayService,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    IDatabaseCacheServiceFactory databaseCacheFactory,
    CalendarContext context,
    ILoggerFactory loggerFactory)
{
    public async Task<WeatherInfo?> GetWeatherAsync(Display display, DateTime date)
    {
        // Try Met.no first
        var metNoWeather = await GetMetNoWeatherAsync(display, date);
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
    public async Task<WeatherInfo?> GetMetNoWeatherAsync(Display display, DateTime date)
    {
        if (!displayService.GetConfigBool(display, "metnoweather"))
            return null;

        var lat = displayService.GetConfigDouble(display, "lat");
        var lon = displayService.GetConfigDouble(display, "lon");
        var alt = displayService.GetConfigDouble(display, "alt");

        if (lat == null || lon == null || alt == null)
        {
            logger.LogWarning("Met.no weather enabled but lat/lon/alt not configured");
            return null;
        }

        try
        {
            var service = new MetNoWeatherService(
                loggerFactory.CreateLogger<MetNoWeatherService>(),
                httpClientFactory,
                memoryCache,
                databaseCacheFactory,
                context,
                lat.Value,
                lon.Value,
                alt.Value
            );

            var detailedForecast = await service.GetForecastAsync();

            var dtStart = date.ToUniversalTime();
            dtStart = new DateTime(dtStart.Year, dtStart.Month, dtStart.Day, dtStart.Hour, 0, 0, DateTimeKind.Utc);

            // Current weather (1 hour aggregate from now)
            var currentWeather = service.Aggregate(detailedForecast, dtStart, 1);

            // Forecast (multiple aggregated periods)
            var aggregateHours = displayService.GetConfigInt(display, "metnoweather_granularity_hours") ?? 2;
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
            throw;
        }
    }

    /// <summary>
    /// Get current weather and forecast from OpenWeatherMap
    /// </summary>
    public async Task<OpenWeatherInfo?> GetOpenWeatherAsync(Display display)
    {
        if (!displayService.GetConfigBool(display, "openweather"))
            return null;

        var apiKey = displayService.GetConfig(display, "openweather_api_key");
        var lat = displayService.GetConfigDouble(display, "lat");
        var lon = displayService.GetConfigDouble(display, "lon");
        var lang = displayService.GetConfig(display, "openweather_lang") ?? "en";

        if (string.IsNullOrWhiteSpace(apiKey) || lat == null || lon == null)
        {
            logger.LogWarning("OpenWeather enabled but API key or lat/lon not configured");
            return null;
        }

        try
        {
            var service = new OpenWeatherService(
                loggerFactory.CreateLogger<OpenWeatherService>(),
                httpClientFactory,
                memoryCache,
                databaseCacheFactory,
                context,
                apiKey,
                lat.Value,
                lon.Value,
                lang
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
            throw;
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
