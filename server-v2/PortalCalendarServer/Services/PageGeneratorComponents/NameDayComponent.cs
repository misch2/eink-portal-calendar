using System.Globalization;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for retrieving Czech name days (Svatky).
/// Delegates to NameDayService for actual name day lookups.
/// </summary>
public class NameDayComponent(
    ILogger<PageGeneratorService> logger,
    NameDayService nameDayService)
{
    /// <summary>
    /// Get name day information for the specified date in Czech calendar.
    /// Returns null if no name day is celebrated on this date.
    /// </summary>
    public NameDayInfo? GetNameDayInfo(DateTime date)
    {
        logger.LogDebug("Getting name day information for {Date}", date);
        return nameDayService.GetNameDay(date);
    }

    /// <summary>
    /// Get all name days for the specified month
    /// </summary>
    public List<NameDayInfo> GetMonthNameDays(DateTime date)
    {
        return nameDayService.GetNameDaysForMonth(date.Year, date.Month);
    }
}
