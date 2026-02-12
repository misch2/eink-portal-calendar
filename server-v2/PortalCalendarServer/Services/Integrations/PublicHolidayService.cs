using PublicHoliday;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Interface for public holiday service
/// </summary>
public interface IPublicHolidayService
{
    PublicHolidayInfo? GetPublicHoliday(DateTime date, string countryCode = "CZ");
    List<PublicHolidayInfo> GetPublicHolidaysForYear(int year, string countryCode = "CZ");
    List<PublicHolidayInfo> GetPublicHolidaysBetween(DateTime startDate, DateTime endDate, string countryCode = "CZ");
    bool IsPublicHoliday(DateTime date, string countryCode = "CZ");
    PublicHolidayInfo? GetNextPublicHoliday(DateTime date, string countryCode = "CZ");
}

/// <summary>
/// Service for retrieving public holiday information.
/// Currently supports Czech Republic holidays using the PublicHoliday library.
/// </summary>
public class PublicHolidayService : IPublicHolidayService
{
    private readonly ILogger<PublicHolidayService> _logger;

    // Czech holiday names mapping (date format: MM-dd)
    private static readonly Dictionary<string, (string English, string Czech)> CzechHolidayNames = new()
    {
        { "01-01", ("New Year's Day", "Nový rok") },
        { "03-29", ("Good Friday", "Velký pátek") },  // Movable - will be set dynamically
        { "04-01", ("Easter Monday", "Velikonoèní pondìlí") },  // Movable - will be set dynamically  
        { "05-01", ("Labour Day", "Svátek práce") },
        { "05-08", ("Liberation Day", "Den vítìzství") },
        { "07-05", ("Saints Cyril and Methodius Day", "Den slovanských vìrozvìstù Cyrila a Metodìje") },
        { "07-06", ("Jan Hus Day", "Den upálení mistra Jana Husa") },
        { "09-28", ("St. Wenceslas Day", "Den èeské státnosti") },
        { "10-28", ("Independent Czechoslovak State Day", "Den vzniku samostatného èeskoslovenského státu") },
        { "11-17", ("Struggle for Freedom and Democracy Day", "Den boje za svobodu a demokracii") },
        { "12-24", ("Christmas Eve", "Štìdrý den") },
        { "12-25", ("Christmas Day", "1. svátek vánoèní") },
        { "12-26", ("St. Stephen's Day", "2. svátek vánoèní") }
    };

    public PublicHolidayService(ILogger<PublicHolidayService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get public holiday information for a specific date
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <param name="countryCode">Country code (currently only "CZ" is supported)</param>
    /// <returns>Public holiday information or null if not a holiday</returns>
    public PublicHolidayInfo? GetPublicHoliday(DateTime date, string countryCode = "CZ")
    {
        if (countryCode != "CZ")
        {
            _logger.LogWarning("Unsupported country code: {CountryCode}. Only 'CZ' is currently supported.", countryCode);
            return null;
        }

        _logger.LogDebug("Checking if {Date} is a public holiday in {CountryCode}", date, countryCode);

        var provider = new CzechRepublicPublicHoliday();

        if (!provider.IsPublicHoliday(date))
        {
            _logger.LogDebug("{Date} is not a public holiday", date);
            return null;
        }

        var holidays = provider.PublicHolidaysInformation(date.Year);
        var holiday = holidays.FirstOrDefault(h => h.HolidayDate.Date == date.Date);

        if (holiday == null)
        {
            _logger.LogWarning("Date {Date} was identified as public holiday but no holiday information found", date);
            return null;
        }

        // Get names from our mapping, or use library values as fallback
        var dateKey = date.ToString("MM-dd");
        var (englishName, czechName) = CzechHolidayNames.GetValueOrDefault(dateKey,
            (holiday.EnglishName ?? "Public Holiday", holiday.Name ?? "Státní svátek"));

        _logger.LogDebug("Found public holiday: {EnglishName} ({CzechName}) on {Date}", 
            englishName, czechName, date);

        return new PublicHolidayInfo
        {
            Name = englishName,
            LocalName = czechName,
            Date = holiday.HolidayDate,
            CountryCode = countryCode
        };
    }

    /// <summary>
    /// Get all public holidays for a specific year
    /// </summary>
    /// <param name="year">The year to query</param>
    /// <param name="countryCode">Country code (currently only "CZ" is supported)</param>
    /// <returns>List of public holidays in the year</returns>
    public List<PublicHolidayInfo> GetPublicHolidaysForYear(int year, string countryCode = "CZ")
    {
        if (countryCode != "CZ")
        {
            _logger.LogWarning("Unsupported country code: {CountryCode}", countryCode);
            return new List<PublicHolidayInfo>();
        }

        _logger.LogDebug("Getting all public holidays for year {Year} in {CountryCode}", year, countryCode);

        var provider = new CzechRepublicPublicHoliday();
        var holidays = provider.PublicHolidaysInformation(year);

        var result = new List<PublicHolidayInfo>();
        foreach (var h in holidays)
        {
            var dateKey = h.HolidayDate.ToString("MM-dd");
            var (englishName, czechName) = CzechHolidayNames.GetValueOrDefault(dateKey,
                (h.EnglishName ?? "Public Holiday", h.Name ?? "Státní svátek"));

            result.Add(new PublicHolidayInfo
            {
                Name = englishName,
                LocalName = czechName,
                Date = h.HolidayDate,
                CountryCode = countryCode
            });
        }

        return result.OrderBy(h => h.Date).ToList();
    }

    /// <summary>
    /// Get all public holidays within a date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="countryCode">Country code (currently only "CZ" is supported)</param>
    /// <returns>List of public holidays in the date range</returns>
    public List<PublicHolidayInfo> GetPublicHolidaysBetween(DateTime startDate, DateTime endDate, string countryCode = "CZ")
    {
        if (countryCode != "CZ")
        {
            _logger.LogWarning("Unsupported country code: {CountryCode}", countryCode);
            return new List<PublicHolidayInfo>();
        }

        _logger.LogDebug("Getting public holidays between {StartDate} and {EndDate} in {CountryCode}", 
            startDate, endDate, countryCode);

        var allHolidays = new List<PublicHolidayInfo>();

        // Get holidays for all years in the range
        for (int year = startDate.Year; year <= endDate.Year; year++)
        {
            allHolidays.AddRange(GetPublicHolidaysForYear(year, countryCode));
        }

        // Filter to the specific date range
        return allHolidays
            .Where(h => h.Date.Date >= startDate.Date && h.Date.Date <= endDate.Date)
            .OrderBy(h => h.Date)
            .ToList();
    }

    /// <summary>
    /// Check if a specific date is a public holiday
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <param name="countryCode">Country code (currently only "CZ" is supported)</param>
    /// <returns>True if the date is a public holiday, false otherwise</returns>
    public bool IsPublicHoliday(DateTime date, string countryCode = "CZ")
    {
        if (countryCode != "CZ")
        {
            _logger.LogWarning("Unsupported country code: {CountryCode}", countryCode);
            return false;
        }

        var provider = new CzechRepublicPublicHoliday();
        return provider.IsPublicHoliday(date);
    }

    /// <summary>
    /// Get the next public holiday after a specific date
    /// </summary>
    /// <param name="date">The date to start searching from</param>
    /// <param name="countryCode">Country code (currently only "CZ" is supported)</param>
    /// <returns>The next public holiday or null if none found in the current or next year</returns>
    public PublicHolidayInfo? GetNextPublicHoliday(DateTime date, string countryCode = "CZ")
    {
        if (countryCode != "CZ")
        {
            _logger.LogWarning("Unsupported country code: {CountryCode}", countryCode);
            return null;
        }

        _logger.LogDebug("Finding next public holiday after {Date} in {CountryCode}", date, countryCode);

        // Check current year
        var currentYearHolidays = GetPublicHolidaysForYear(date.Year, countryCode);
        var nextInCurrentYear = currentYearHolidays.FirstOrDefault(h => h.Date.Date > date.Date);

        if (nextInCurrentYear != null)
        {
            return nextInCurrentYear;
        }

        // Check next year
        var nextYearHolidays = GetPublicHolidaysForYear(date.Year + 1, countryCode);
        return nextYearHolidays.FirstOrDefault();
    }
}

/// <summary>
/// Public holiday information
/// </summary>
public class PublicHolidayInfo
{
    /// <summary>
    /// English name of the holiday
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Local name of the holiday (in the country's language)
    /// </summary>
    public string LocalName { get; set; } = string.Empty;

    /// <summary>
    /// Date of the holiday
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Country code (e.g., "CZ" for Czech Republic)
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Additional description or notes about the holiday
    /// </summary>
    public string? Description { get; set; }
}
