using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for retrieving public holiday information.
/// Delegates to IPublicHolidayService for actual holiday lookups.
/// </summary>
public class PublicHolidayComponent : BaseComponent
{
    private readonly IPublicHolidayService _publicHolidayService;

    public PublicHolidayComponent(
        ILogger<PageGeneratorService> logger,
        DisplayService displayService,
        IPublicHolidayService publicHolidayService)
        : base(logger, displayService)
    {
        _publicHolidayService = publicHolidayService;
    }

    /// <summary>
    /// Get public holiday information for the specified date.
    /// Returns null if the date is not a public holiday.
    /// </summary>
    public PublicHolidayInfo? GetPublicHolidayInfo(DateTime date)
    {
        _logger.LogDebug("Getting public holiday information for {Date}", date);
        return _publicHolidayService.GetPublicHoliday(date);
    }

    /// <summary>
    /// Get all public holidays for the year of the specified date
    /// </summary>
    public List<PublicHolidayInfo> GetYearHolidays(DateTime date)
    {
        return _publicHolidayService.GetPublicHolidaysForYear(date.Year);
    }

    /// <summary>
    /// Check if the specified date is a public holiday
    /// </summary>
    public bool IsPublicHoliday(DateTime date)
    {
        return _publicHolidayService.IsPublicHoliday(date);
    }

    /// <summary>
    /// Get the next public holiday after the specified date
    /// </summary>
    public PublicHolidayInfo? GetNextHoliday(DateTime date)
    {
        return _publicHolidayService.GetNextPublicHoliday(date);
    }
}
