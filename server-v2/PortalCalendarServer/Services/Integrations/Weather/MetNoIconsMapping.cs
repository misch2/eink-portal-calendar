namespace PortalCalendarServer.Services.Integrations.Weather;

/// <summary>
/// Maps Met.no weather symbol codes to Weather Icons codes and descriptions.
/// Based on PortalCalendar::Integration::Weather::MetNo::IconsMapping from Perl.
/// </summary>
public class MetNoIconsMapping
{
    private readonly Dictionary<string, WeatherSymbol> _symbolMap;

    public MetNoIconsMapping()
    {
        _symbolMap = InitializeSymbolMap();
    }

    private static Dictionary<string, WeatherSymbol> InitializeSymbolMap()
    {
        var symbols = new List<WeatherSymbol>
        {
            new() { Code = "clearsky", DescriptionEn = "Clear sky", DescriptionCz = "Jasno", OpenWeatherId = 800 },
            new() { Code = "fair", DescriptionEn = "Fair", DescriptionCz = "Polojasno", OpenWeatherId = 801 },
            new() { Code = "partlycloudy", DescriptionEn = "Partly cloudy", DescriptionCz = "Polojasno", OpenWeatherId = 801 },
            new() { Code = "cloudy", DescriptionEn = "Cloudy", DescriptionCz = "Zataženo", OpenWeatherId = 804 },
            new() { Code = "lightrainshowers", DescriptionEn = "Light rain showers", DescriptionCz = "Slabé pøeháòky", OpenWeatherId = 500 },
            new() { Code = "rainshowers", DescriptionEn = "Rain showers", DescriptionCz = "Pøeháòky", OpenWeatherId = 501 },
            new() { Code = "heavyrainshowers", DescriptionEn = "Heavy rain showers", DescriptionCz = "Silné pøeháòky", OpenWeatherId = 502 },
            new() { Code = "lightrainshowersandthunder", DescriptionEn = "Light rain showers and thunder", DescriptionCz = "Slabé pøeháòky a bouøky", OpenWeatherId = 200 },
            new() { Code = "rainshowersandthunder", DescriptionEn = "Rain showers and thunder", DescriptionCz = "Pøeháòky a bouøky", OpenWeatherId = 201 },
            new() { Code = "heavyrainshowersandthunder", DescriptionEn = "Heavy rain showers and thunder", DescriptionCz = "Silné pøeháòky a bouøky", OpenWeatherId = 202 },
            new() { Code = "lightsleetshowers", DescriptionEn = "Light sleet showers", DescriptionCz = "Slabé déš se snìhem", OpenWeatherId = 611 },
            new() { Code = "sleetshowers", DescriptionEn = "Sleet showers", DescriptionCz = "Déš se snìhem", OpenWeatherId = 612 },
            new() { Code = "heavysleetshowers", DescriptionEn = "Heavy sleet showers", DescriptionCz = "Silný déš se snìhem", OpenWeatherId = 615 },
            new() { Code = "lightssleetshowersandthunder", DescriptionEn = "Light sleet showers and thunder", DescriptionCz = "Slabé déš se snìhem a bouøky", OpenWeatherId = 210 },
            new() { Code = "sleetshowersandthunder", DescriptionEn = "Sleet showers and thunder", DescriptionCz = "Déš se snìhem a bouøky", OpenWeatherId = 211 },
            new() { Code = "heavysleetshowersandthunder", DescriptionEn = "Heavy sleet showers and thunder", DescriptionCz = "Silný déš se snìhem a bouøky", OpenWeatherId = 212 },
            new() { Code = "lightsnowshowers", DescriptionEn = "Light snow showers", DescriptionCz = "Slabé snìhové pøeháòky", OpenWeatherId = 600 },
            new() { Code = "snowshowers", DescriptionEn = "Snow showers", DescriptionCz = "Snìhové pøeháòky", OpenWeatherId = 601 },
            new() { Code = "heavysnowshowers", DescriptionEn = "Heavy snow showers", DescriptionCz = "Silné snìhové pøeháòky", OpenWeatherId = 602 },
            new() { Code = "lightssnowshowersandthunder", DescriptionEn = "Light snow showers and thunder", DescriptionCz = "Slabé snìhové pøeháòky a bouøky", OpenWeatherId = 230 },
            new() { Code = "snowshowersandthunder", DescriptionEn = "Snow showers and thunder", DescriptionCz = "Snìhové pøeháòky a bouøky", OpenWeatherId = 231 },
            new() { Code = "heavysnowshowersandthunder", DescriptionEn = "Heavy snow showers and thunder", DescriptionCz = "Silné snìhové pøeháòky a bouøky", OpenWeatherId = 232 },
            new() { Code = "lightrain", DescriptionEn = "Light rain", DescriptionCz = "Slabý déš", OpenWeatherId = 300 },
            new() { Code = "rain", DescriptionEn = "Rain", DescriptionCz = "Déš", OpenWeatherId = 301 },
            new() { Code = "heavyrain", DescriptionEn = "Heavy rain", DescriptionCz = "Silný déš", OpenWeatherId = 302 },
            new() { Code = "lightrainandthunder", DescriptionEn = "Light rain and thunder", DescriptionCz = "Slabý déš a bouøky", OpenWeatherId = 210 },
            new() { Code = "rainandthunder", DescriptionEn = "Rain and thunder", DescriptionCz = "Déš a bouøky", OpenWeatherId = 211 },
            new() { Code = "heavyrainandthunder", DescriptionEn = "Heavy rain and thunder", DescriptionCz = "Silný déš a bouøky", OpenWeatherId = 212 },
            new() { Code = "lightsleet", DescriptionEn = "Light sleet", DescriptionCz = "Slabý déš se snìhem", OpenWeatherId = 611 },
            new() { Code = "sleet", DescriptionEn = "Sleet", DescriptionCz = "Déš se snìhem", OpenWeatherId = 612 },
            new() { Code = "heavysleet", DescriptionEn = "Heavy sleet", DescriptionCz = "Silný déš se snìhem", OpenWeatherId = 615 },
            new() { Code = "lightsleetandthunder", DescriptionEn = "Light sleet and thunder", DescriptionCz = "Slabý déš se snìhem a bouøky", OpenWeatherId = 221 },
            new() { Code = "sleetandthunder", DescriptionEn = "Sleet and thunder", DescriptionCz = "Déš se snìhem a bouøky", OpenWeatherId = 221 },
            new() { Code = "heavysleetandthunder", DescriptionEn = "Heavy sleet and thunder", DescriptionCz = "Silný déš se snìhem a bouøky", OpenWeatherId = 221 },
            new() { Code = "lightsnow", DescriptionEn = "Light snow", DescriptionCz = "Slabý sníh", OpenWeatherId = 600 },
            new() { Code = "snow", DescriptionEn = "Snow", DescriptionCz = "Sníh", OpenWeatherId = 601 },
            new() { Code = "heavysnow", DescriptionEn = "Heavy snow", DescriptionCz = "Silný sníh", OpenWeatherId = 602 },
            new() { Code = "lightsnowandthunder", DescriptionEn = "Light snow and thunder", DescriptionCz = "Slabý sníh a bouøky", OpenWeatherId = 230 },
            new() { Code = "snowandthunder", DescriptionEn = "Snow and thunder", DescriptionCz = "Sníh a bouøky", OpenWeatherId = 231 },
            new() { Code = "heavysnowandthunder", DescriptionEn = "Heavy snow and thunder", DescriptionCz = "Silný sníh a bouøky", OpenWeatherId = 232 },
            new() { Code = "fog", DescriptionEn = "Fog", DescriptionCz = "Mlha", OpenWeatherId = 741 }
        };

        return symbols.ToDictionary(s => s.Code, s => s);
    }

    /// <summary>
    /// Get symbol details for a given code
    /// </summary>
    public WeatherSymbol? GetSymbolDetails(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        // Remove day/night/polartwilight suffix
        var normalizedCode = code;
        if (code.EndsWith("_day") || code.EndsWith("_night") || code.EndsWith("_polartwilight"))
        {
            var lastUnderscore = code.LastIndexOf('_');
            normalizedCode = code.Substring(0, lastUnderscore);
        }

        return _symbolMap.GetValueOrDefault(normalizedCode);
    }

    /// <summary>
    /// Map symbol code to OpenWeatherMap ID
    /// </summary>
    public int? MapSymbol(string? code)
    {
        var details = GetSymbolDetails(code);
        return details?.OpenWeatherId;
    }

    /// <summary>
    /// Map symbol code to localized description
    /// </summary>
    public string? MapDescription(string? code, string language = "cz")
    {
        var details = GetSymbolDetails(code);
        if (details == null)
            return null;

        return language.ToLower() switch
        {
            "cz" or "cs" => details.DescriptionCz,
            "en" => details.DescriptionEn,
            _ => details.DescriptionCz
        };
    }
}

/// <summary>
/// Weather symbol definition
/// </summary>
public class WeatherSymbol
{
    public string Code { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionCz { get; set; } = string.Empty;
    public int OpenWeatherId { get; set; }
}
