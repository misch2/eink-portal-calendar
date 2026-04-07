using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for retrieving name days.
/// Delegates to NameDayService for actual name day lookups.
/// </summary>
public class NameDayComponent(
    ILogger<PageGeneratorService> logger,
    INameDayService nameDayService,
    string countryCode)
{
    /// <summary>
    /// Get name day information for the specified date.
    /// Returns null if no name day is celebrated on this date or if the country is not supported.
    /// </summary>
    public NameDayInfo? GetNameDayInfo(DateTime date)
    {
        logger.LogDebug("Getting name day information for {Date} in {CountryCode}", date, countryCode);
        return nameDayService.GetNameDay(date, countryCode);
    }

    /// <summary>
    /// Get all name days for the specified month
    /// </summary>
    public List<NameDayInfo> GetMonthNameDays(DateTime date)
    {
        return nameDayService.GetNameDaysForMonth(date.Year, date.Month, countryCode);
    }
}
