using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for retrieving public holiday information.
/// Delegates to PublicHolidayService for actual holiday lookups.
/// </summary>
public class PublicHolidayComponent : BaseComponent
{
    private readonly IPublicHolidayService _publicHolidayService;

    public PublicHolidayComponent(
        ILogger<PageGeneratorService> logger,
        DisplayService? displayService,
        DateTime date,
        IPublicHolidayService publicHolidayService)
        : base(logger, displayService, date)
    {
        _publicHolidayService = publicHolidayService;
    }

    /// <summary>
    /// Get public holiday information for the current date.
    /// Returns null if the date is not a public holiday.
    /// </summary>
    public PublicHolidayInfo? GetPublicHolidayInfo()
    {
        _logger.LogDebug("Getting public holiday information for {Date}", _date);
        return _publicHolidayService.GetPublicHoliday(_date);
    }

    /// <summary>
    /// Get all public holidays for the current year
    /// </summary>
    public List<PublicHolidayInfo> GetYearHolidays()
    {
        return _publicHolidayService.GetPublicHolidaysForYear(_date.Year);
    }

    /// <summary>
    /// Check if the current date is a public holiday
    /// </summary>
    public bool IsPublicHoliday()
    {
        return _publicHolidayService.IsPublicHoliday(_date);
    }

    /// <summary>
    /// Get the next public holiday after the current date
    /// </summary>
    public PublicHolidayInfo? GetNextHoliday()
    {
        return _publicHolidayService.GetNextPublicHoliday(_date);
    }
}
