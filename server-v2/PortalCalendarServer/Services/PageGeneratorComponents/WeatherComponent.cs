using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models;
using PortalCalendarServer.Models.Weather;
using PortalCalendarServer.Services.Integrations.Weather;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for weather data
/// </summary>
public class WeatherComponent : BaseComponent
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly CalendarContext _context;
    private readonly ILoggerFactory _loggerFactory;

    public WeatherComponent(
        ILogger<PageGeneratorService> logger,
        DisplayService displayService,
        DateTime date,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        CalendarContext context,
        ILoggerFactory loggerFactory)
        : base(logger, displayService, date)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _context = context;
        _loggerFactory = loggerFactory;
    }

    public async Task<WeatherInfo?> GetWeatherAsync()
    {
        // Try Met.no first
        var metNoWeather = await GetMetNoWeatherAsync();
        if (metNoWeather != null)
        {
            return metNoWeather;
        }
        // FIXME add fallback to OpenWeatherMap
        return null;
    }

    /// <summary>
    /// Get current weather and forecast from Met.no
    /// </summary>
    public async Task<WeatherInfo?> GetMetNoWeatherAsync()
    {
        if (!_displayService.GetConfigBool("metnoweather"))
            return null;

        var lat = _displayService.GetConfigDouble("lat");
        var lon = _displayService.GetConfigDouble("lon");
        var alt = _displayService.GetConfigDouble("alt");

        if (lat == null || lon == null || alt == null)
        {
            _logger.LogWarning("Met.no weather enabled but lat/lon/alt not configured");
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
                _displayService.CurrentDisplay
            );

            var detailedForecast = await service.GetForecastAsync();
            
            var dtStart = _date.ToUniversalTime();
            dtStart = new DateTime(dtStart.Year, dtStart.Month, dtStart.Day, dtStart.Hour, 0, 0, DateTimeKind.Utc);

            // Current weather (1 hour aggregate from now)
            var currentWeather = service.Aggregate(detailedForecast, dtStart, 1);

            // Forecast (multiple aggregated periods)
            var aggregateHours = _displayService.GetConfigInt("metnoweather_granularity_hours") ?? 2;
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
            _logger.LogError(ex, "Error fetching Met.no weather data");
            return null;
        }
    }

    /// <summary>
    /// Get current weather and forecast from OpenWeatherMap
    /// </summary>
    public async Task<OpenWeatherInfo?> GetOpenWeatherAsync()
    {
        if (!_displayService.GetConfigBool("openweather"))
            return null;

        var apiKey = _displayService.GetConfig("openweather_api_key");
        var lat = _displayService.GetConfigDouble("lat");
        var lon = _displayService.GetConfigDouble("lon");
        var lang = _displayService.GetConfig("openweather_lang") ?? "en";

        if (string.IsNullOrEmpty(apiKey) || lat == null || lon == null)
        {
            _logger.LogWarning("OpenWeather enabled but API key or lat/lon not configured");
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
                _displayService.CurrentDisplay
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
            _logger.LogError(ex, "Error fetching OpenWeather data");
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
    public List<AggregatedWeatherData> Forecast { get; set; } = new();
}

/// <summary>
/// Weather information from OpenWeatherMap
/// </summary>
public class OpenWeatherInfo
{
    public OpenWeatherCurrent? Current { get; set; }
    public OpenWeatherForecast? Forecast { get; set; }
}
