using System.Globalization;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for retrieving Czech name days (Svatky).
/// Delegates to NameDayService for actual name day lookups.
/// </summary>
public class NameDayComponent : BaseComponent
{
    private readonly NameDayService _nameDayService;

    public NameDayComponent(
        ILogger<PageGeneratorService> logger,
        DisplayService displayService,
        NameDayService nameDayService)
        : base(logger, displayService)
    {
        _nameDayService = nameDayService;
    }

    /// <summary>
    /// Get name day information for the specified date in Czech calendar.
    /// Returns null if no name day is celebrated on this date.
    /// </summary>
    public NameDayInfo? GetNameDayInfo(DateTime date)
    {
        _logger.LogDebug("Getting name day information for {Date}", date);
        return _nameDayService.GetNameDay(date);
    }

    /// <summary>
    /// Get all name days for the specified month
    /// </summary>
    public List<NameDayInfo> GetMonthNameDays(DateTime date)
    {
        return _nameDayService.GetNameDaysForMonth(date.Year, date.Month);
    }
}
