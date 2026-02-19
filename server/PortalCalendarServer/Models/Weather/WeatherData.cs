namespace PortalCalendarServer.Models.Weather;

/// <summary>
/// Weather data for a specific time period
/// </summary>
public class WeatherData
{
    /// <summary>
    /// Weather provider name (e.g. "met.no", "openweathermap")
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Temperature in Celsius
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Atmospheric pressure at sea level in hPa
    /// </summary>
    public double PressureAtSeaLevel { get; set; }

    /// <summary>
    /// Relative humidity as percentage (0-100)
    /// </summary>
    public double Humidity { get; set; }

    /// <summary>
    /// Cloud coverage as percentage (0-100)
    /// </summary>
    public double CloudPercent { get; set; }

    /// <summary>
    /// Fog coverage as percentage (0-100)
    /// </summary>
    public double FogPercent { get; set; }

    /// <summary>
    /// Wind speed in m/s
    /// </summary>
    public double WindSpeed { get; set; }

    /// <summary>
    /// Wind direction in degrees (0-360, 0 = North)
    /// </summary>
    public double WindFrom { get; set; }

    /// <summary>
    /// Precipitation amount in mm
    /// </summary>
    public double Precipitation { get; set; }

    /// <summary>
    /// Provider-specific weather symbol code
    /// </summary>
    public string ProviderSymbolCode { get; set; } = string.Empty;

    /// <summary>
    /// Weather Icons symbol code (OpenWeatherMap ID)
    /// </summary>
    public int? WiSymbolCode { get; set; }

    /// <summary>
    /// Localized weather description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Start time of this weather period (UTC)
    /// </summary>
    public DateTime TimeStart { get; set; }

    /// <summary>
    /// End time of this weather period (UTC)
    /// </summary>
    public DateTime TimeEnd { get; set; }

    /// <summary>
    /// When this forecast was last updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Whether this time period is during daytime
    /// </summary>
    public bool TimeIsDay { get; set; }
}

/// <summary>
/// Aggregated weather data over multiple time periods
/// </summary>
public class AggregatedWeatherData
{
    /// <summary>
    /// Weather provider name
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Number of time periods aggregated
    /// </summary>
    public int AggregatedCount { get; set; }

    /// <summary>
    /// Minimum temperature in °C
    /// </summary>
    public double TemperatureMin { get; set; }

    /// <summary>
    /// Maximum temperature in °C
    /// </summary>
    public double TemperatureMax { get; set; }

    /// <summary>
    /// Average temperature in °C
    /// </summary>
    public double TemperatureAvg { get; set; }

    /// <summary>
    /// Minimum pressure in hPa
    /// </summary>
    public double PressureMin { get; set; }

    /// <summary>
    /// Maximum pressure in hPa
    /// </summary>
    public double PressureMax { get; set; }

    /// <summary>
    /// Average pressure in hPa
    /// </summary>
    public double PressureAvg { get; set; }

    /// <summary>
    /// Minimum humidity percentage
    /// </summary>
    public double HumidityMin { get; set; }

    /// <summary>
    /// Maximum humidity percentage
    /// </summary>
    public double HumidityMax { get; set; }

    /// <summary>
    /// Average humidity percentage
    /// </summary>
    public double HumidityAvg { get; set; }

    /// <summary>
    /// Minimum cloud coverage percentage
    /// </summary>
    public double CloudPercentMin { get; set; }

    /// <summary>
    /// Maximum cloud coverage percentage
    /// </summary>
    public double CloudPercentMax { get; set; }

    /// <summary>
    /// Average cloud coverage percentage
    /// </summary>
    public double CloudPercentAvg { get; set; }

    /// <summary>
    /// Minimum fog percentage
    /// </summary>
    public double FogPercentMin { get; set; }

    /// <summary>
    /// Maximum fog percentage
    /// </summary>
    public double FogPercentMax { get; set; }

    /// <summary>
    /// Average fog percentage
    /// </summary>
    public double FogPercentAvg { get; set; }

    /// <summary>
    /// Minimum wind speed in m/s
    /// </summary>
    public double WindSpeedMin { get; set; }

    /// <summary>
    /// Maximum wind speed in m/s
    /// </summary>
    public double WindSpeedMax { get; set; }

    /// <summary>
    /// Average wind speed in m/s
    /// </summary>
    public double WindSpeedAvg { get; set; }

    /// <summary>
    /// Average wind direction in degrees
    /// </summary>
    public double WindFrom { get; set; }

    /// <summary>
    /// Minimum precipitation in mm
    /// </summary>
    public double PrecipitationMin { get; set; }

    /// <summary>
    /// Maximum precipitation in mm
    /// </summary>
    public double PrecipitationMax { get; set; }

    /// <summary>
    /// Average precipitation in mm
    /// </summary>
    public double PrecipitationAvg { get; set; }

    /// <summary>
    /// Total precipitation sum in mm
    /// </summary>
    public double PrecipitationSum { get; set; }

    /// <summary>
    /// All provider symbol codes in this period
    /// </summary>
    public List<string> ProviderSymbolCodes { get; set; } = new();

    /// <summary>
    /// All Weather Icons symbol codes in this period
    /// </summary>
    public List<int> WiSymbolCodes { get; set; } = new();

    /// <summary>
    /// All weather descriptions in this period
    /// </summary>
    public List<string> Descriptions { get; set; } = new();

    /// <summary>
    /// Start time of aggregated period (UTC)
    /// </summary>
    public DateTime TimeStart { get; set; }

    /// <summary>
    /// End time of aggregated period (UTC)
    /// </summary>
    public DateTime TimeEnd { get; set; }

    /// <summary>
    /// When this forecast was last updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Whether this period is during daytime
    /// </summary>
    public bool TimeIsDay { get; set; }
}
