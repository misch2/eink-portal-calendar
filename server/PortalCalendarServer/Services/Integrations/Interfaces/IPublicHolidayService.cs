namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Interface for public holiday service
/// </summary>
public interface IPublicHolidayService : IIntegrationService
{
    PublicHolidayInfo? GetPublicHoliday(DateTime date, string countryCode);
    List<PublicHolidayInfo> GetPublicHolidaysForYear(int year, string countryCode);
    List<PublicHolidayInfo> GetPublicHolidaysBetween(DateTime startDate, DateTime endDate, string countryCode);
    bool IsPublicHoliday(DateTime date, string countryCode);
    PublicHolidayInfo? GetNextPublicHoliday(DateTime date, string countryCode);
}

/// <summary>
/// Public holiday information
/// </summary>
public class PublicHolidayInfo
{
    /// <summary>
    /// Date of the holiday
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Local name of the holiday (in the country's language)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Additional description or notes about the holiday
    /// </summary>
    public string? Description { get; set; }
}
