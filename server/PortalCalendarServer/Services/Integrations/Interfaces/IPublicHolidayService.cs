namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Interface for public holiday service
/// </summary>
public interface IPublicHolidayService : IIntegrationService
{
    PublicHolidayInfo? GetPublicHoliday(DateTime date, string countryCode = "CZ");
    List<PublicHolidayInfo> GetPublicHolidaysForYear(int year, string countryCode = "CZ");
    List<PublicHolidayInfo> GetPublicHolidaysBetween(DateTime startDate, DateTime endDate, string countryCode = "CZ");
    bool IsPublicHoliday(DateTime date, string countryCode = "CZ");
    PublicHolidayInfo? GetNextPublicHoliday(DateTime date, string countryCode = "CZ");
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
