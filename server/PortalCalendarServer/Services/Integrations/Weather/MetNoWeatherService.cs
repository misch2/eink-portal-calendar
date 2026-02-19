using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Models.Weather;
using PortalCalendarServer.Services.Caches;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortalCalendarServer.Services.Integrations.Weather;

/// <summary>
/// Integration service for Met.no weather API.
/// Based on PortalCalendar::Integration::Weather::MetNo from Perl.
/// </summary>
public class MetNoWeatherService : IntegrationServiceBase
{
    private readonly double _latitude;
    private readonly double _longitude;
    private readonly double _altitude;
    private readonly MetNoIconsMapping _iconMapping;

    public MetNoWeatherService(
        ILogger<MetNoWeatherService> logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        IDatabaseCacheServiceFactory databaseCacheFactory,
        CalendarContext context,
        double latitude,
        double longitude,
        double altitude)
        : base(logger, httpClientFactory, memoryCache, databaseCacheFactory, context)
    {
        _latitude = latitude;
        _longitude = longitude;
        _altitude = altitude;
        _iconMapping = new MetNoIconsMapping();
    }

    public override bool IsConfigured(Display display)
    {
        return _latitude != 0 && _longitude != 0;
    }

    private string GetUrl()
    {
        // Truncate coordinates to max 3 decimals (Met.no requirement)
        var lat = Math.Round(_latitude, 3);
        var lon = Math.Round(_longitude, 3);

        return $"https://api.met.no/weatherapi/locationforecast/2.0/complete?lat={lat:F3}&lon={lon:F3}&altitude={_altitude}";
    }

    /// <summary>
    /// Fetch raw JSON from Met.no API
    /// </summary>
    private async Task<MetNoResponse> GetRawJsonFromWebAsync(CancellationToken cancellationToken = default)
    {
        var url = GetUrl();
        var cacheKey = $"metno:json:{url}";

        // Try memory cache first
        if (memoryCache.TryGetValue<MetNoResponse>(cacheKey, out var cached) && cached != null)
        {
            logger.LogDebug("Met.no JSON cache HIT");
            return cached;
        }

        logger.LogDebug("Met.no JSON cache MISS, fetching from {Url}", url);

        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<MetNoResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize Met.no response");

        // Cache for 15 minutes
        memoryCache.Set(cacheKey, data, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
            Size = 1024 // Rough estimate in bytes
        });

        return data;
    }

    private WeatherData? ExtractData(MetNoResponse fullResponse, MetNoTimeseries timeseries)
    {
        // Skip forecasts without hourly data
        if (timeseries.Data?.Next1Hours?.Summary == null)
            return null;

        var timeStart = DateTime.Parse(timeseries.Time).ToUniversalTime();
        var timeEnd = timeStart.AddHours(1);

        var instant = timeseries.Data.Instant?.Details;
        var next1h = timeseries.Data.Next1Hours;

        if (instant == null || next1h == null)
            return null;

        var symbolCode = next1h.Summary?.SymbolCode ?? string.Empty;
        var isDay = IsDaytime(timeStart, _latitude, _longitude);

        return new WeatherData
        {
            Provider = "met.no",
            Temperature = instant.AirTemperature ?? 0,
            PressureAtSeaLevel = instant.AirPressureAtSeaLevel ?? 0,
            Humidity = instant.RelativeHumidity ?? 0,
            CloudPercent = instant.CloudAreaFraction ?? 0,
            FogPercent = instant.FogAreaFraction ?? 0,
            WindSpeed = instant.WindSpeed ?? 0,
            WindFrom = instant.WindFromDirection ?? 0,
            Precipitation = next1h.Details?.PrecipitationAmount ?? 0,
            ProviderSymbolCode = symbolCode,
            WiSymbolCode = _iconMapping.MapSymbol(symbolCode),
            Description = _iconMapping.MapDescription(symbolCode) ?? string.Empty,
            TimeStart = timeStart,
            TimeEnd = timeEnd,
            TimeIsDay = isDay,
            UpdatedAt = DateTime.Parse(fullResponse.Properties?.Meta?.UpdatedAt ?? DateTime.UtcNow.ToString()).ToUniversalTime()
        };
    }

    /// <summary>
    /// Get current weather
    /// </summary>
    public async Task<WeatherData?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var json = await GetRawJsonFromWebAsync(cancellationToken);
        var current = json.Properties?.Timeseries?.FirstOrDefault();

        if (current == null)
            return null;

        return ExtractData(json, current);
    }

    /// <summary>
    /// Get forecast
    /// </summary>
    public async Task<List<WeatherData>> GetForecastAsync(CancellationToken cancellationToken = default)
    {
        var json = await GetRawJsonFromWebAsync(cancellationToken);
        var forecast = new List<WeatherData>();

        foreach (var timeseries in json.Properties?.Timeseries ?? Enumerable.Empty<MetNoTimeseries>())
        {
            var data = ExtractData(json, timeseries);
            if (data != null)
            {
                forecast.Add(data);
            }
        }

        return forecast;
    }

    /// <summary>
    /// Aggregate weather data over a time period
    /// </summary>
    public AggregatedWeatherData? Aggregate(List<WeatherData> forecast, DateTime start, int hours)
    {
        var end = start.AddHours(hours);

        var usable = forecast
            .Where(f => f.TimeEnd > start && f.TimeStart < end)
            .ToList();

        if (usable.Count == 0)
            return null;

        var startIsDay = IsDaytime(start, _latitude, _longitude);
        var endIsDay = IsDaytime(end, _latitude, _longitude);

        return new AggregatedWeatherData
        {
            Provider = usable[0].Provider,
            AggregatedCount = usable.Count,

            TemperatureMin = usable.Min(w => w.Temperature),
            TemperatureMax = usable.Max(w => w.Temperature),
            TemperatureAvg = usable.Average(w => w.Temperature),

            PressureMin = usable.Min(w => w.PressureAtSeaLevel),
            PressureMax = usable.Max(w => w.PressureAtSeaLevel),
            PressureAvg = usable.Average(w => w.PressureAtSeaLevel),

            HumidityMin = usable.Min(w => w.Humidity),
            HumidityMax = usable.Max(w => w.Humidity),
            HumidityAvg = usable.Average(w => w.Humidity),

            CloudPercentMin = usable.Min(w => w.CloudPercent),
            CloudPercentMax = usable.Max(w => w.CloudPercent),
            CloudPercentAvg = usable.Average(w => w.CloudPercent),

            FogPercentMin = usable.Min(w => w.FogPercent),
            FogPercentMax = usable.Max(w => w.FogPercent),
            FogPercentAvg = usable.Average(w => w.FogPercent),

            WindSpeedMin = usable.Min(w => w.WindSpeed),
            WindSpeedMax = usable.Max(w => w.WindSpeed),
            WindSpeedAvg = usable.Average(w => w.WindSpeed),
            WindFrom = usable.Average(w => w.WindFrom),

            PrecipitationMin = usable.Min(w => w.Precipitation),
            PrecipitationMax = usable.Max(w => w.Precipitation),
            PrecipitationAvg = usable.Average(w => w.Precipitation),
            PrecipitationSum = usable.Sum(w => w.Precipitation),

            ProviderSymbolCodes = usable.Select(w => w.ProviderSymbolCode).Distinct().ToList(),
            WiSymbolCodes = usable.Select(w => w.WiSymbolCode).Where(c => c.HasValue).Select(c => c!.Value).Distinct().ToList(),
            Descriptions = usable.Select(w => w.Description).Distinct().ToList(),

            TimeStart = usable.Min(w => w.TimeStart),
            TimeEnd = usable.Max(w => w.TimeEnd),
            UpdatedAt = usable[0].UpdatedAt,
            TimeIsDay = startIsDay || endIsDay // Prefer day over night for longer periods
        };
    }

    /// <summary>
    /// Simple day/night calculation based on solar noon approximation
    /// </summary>
    private static bool IsDaytime(DateTime time, double latitude, double longitude)
    {
        // This is a simplified calculation. For production, consider using a library like SunCalc
        var dayOfYear = time.DayOfYear;
        var solarDeclination = 23.45 * Math.Sin((360.0 / 365.0) * (dayOfYear - 81) * Math.PI / 180);

        var hourAngle = 15 * (time.Hour + time.Minute / 60.0 - 12);
        var solarElevation = Math.Asin(
            Math.Sin(latitude * Math.PI / 180) * Math.Sin(solarDeclination * Math.PI / 180) +
            Math.Cos(latitude * Math.PI / 180) * Math.Cos(solarDeclination * Math.PI / 180) * Math.Cos(hourAngle * Math.PI / 180)
        ) * 180 / Math.PI;

        return solarElevation > -6; // Civil twilight
    }
}

// JSON response models for Met.no API
public class MetNoResponse
{
    [JsonPropertyName("properties")]
    public MetNoProperties? Properties { get; set; }
}

public class MetNoProperties
{
    [JsonPropertyName("meta")]
    public MetNoMeta? Meta { get; set; }

    [JsonPropertyName("timeseries")]
    public List<MetNoTimeseries>? Timeseries { get; set; }
}

public class MetNoMeta
{
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}

public class MetNoTimeseries
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public MetNoData? Data { get; set; }
}

public class MetNoData
{
    [JsonPropertyName("instant")]
    public MetNoInstant? Instant { get; set; }

    [JsonPropertyName("next_1_hours")]
    public MetNoNext1Hours? Next1Hours { get; set; }
}

public class MetNoInstant
{
    [JsonPropertyName("details")]
    public MetNoInstantDetails? Details { get; set; }
}

public class MetNoInstantDetails
{
    [JsonPropertyName("air_temperature")]
    public double? AirTemperature { get; set; }

    [JsonPropertyName("air_pressure_at_sea_level")]
    public double? AirPressureAtSeaLevel { get; set; }

    [JsonPropertyName("relative_humidity")]
    public double? RelativeHumidity { get; set; }

    [JsonPropertyName("cloud_area_fraction")]
    public double? CloudAreaFraction { get; set; }

    [JsonPropertyName("fog_area_fraction")]
    public double? FogAreaFraction { get; set; }

    [JsonPropertyName("wind_speed")]
    public double? WindSpeed { get; set; }

    [JsonPropertyName("wind_from_direction")]
    public double? WindFromDirection { get; set; }
}

public class MetNoNext1Hours
{
    [JsonPropertyName("summary")]
    public MetNoSummary? Summary { get; set; }

    [JsonPropertyName("details")]
    public MetNoNext1HoursDetails? Details { get; set; }
}

public class MetNoSummary
{
    [JsonPropertyName("symbol_code")]
    public string? SymbolCode { get; set; }
}

public class MetNoNext1HoursDetails
{
    [JsonPropertyName("precipitation_amount")]
    public double? PrecipitationAmount { get; set; }
}
