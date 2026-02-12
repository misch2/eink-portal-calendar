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
        DisplayService? displayService,
        DateTime date,
        NameDayService nameDayService)
        : base(logger, displayService, date)
    {
        _nameDayService = nameDayService;
    }

    /// <summary>
    /// Get name day information for the current date in Czech calendar.
    /// Returns null if no name day is celebrated on this date.
    /// </summary>
    public NameDayInfo? GetNameDayInfo()
    {
        _logger.LogDebug("Getting name day information for {Date}", _date);
        return _nameDayService.GetNameDay(_date);
    }

    /// <summary>
    /// Get all name days for the current month
    /// </summary>
    public List<NameDayInfo> GetMonthNameDays()
    {
        return _nameDayService.GetNameDaysForMonth(_date.Year, _date.Month);
    }

    /// <summary>
    /// Search for a specific name in the current year
    /// </summary>
    public List<NameDayInfo> SearchName(string name)
    {
        return _nameDayService.FindNameDays(name, _date.Year);
    }
}
