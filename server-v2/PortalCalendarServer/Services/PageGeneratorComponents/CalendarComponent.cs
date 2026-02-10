namespace PortalCalendarServer.Services.PageGeneratorComponents;

public class CalendarInfo
{
    public List<CalendarEvent> TodayEvents { get; set; } = new();
    public List<CalendarEvent> NearestEvents { get; set; } = new();
    public Dictionary<string, List<CalendarEvent>> NearestEventsGrouped { get; set; } = new();
    public bool HasEntries { get; set; }
}

public class CalendarComponent : BaseComponent
{
    public CalendarComponent(
        ILogger<PageGeneratorService> logger,
        DisplayService displayService,
        DateTime date) : base(logger, displayService, date)
    {
    }

    /// <summary>
    /// Get calendar component (icons and events)
    /// </summary>
    public CalendarInfo Details()
    {
        var todayEvents = new List<CalendarEvent>();
        var nearestEvents = new List<CalendarEvent>();

        // Load calendar events from up to 3 ICS calendars
        for (int calendarNo = 1; calendarNo <= 3; calendarNo++)
        {
            var enabled = _displayService.GetConfigBool($"web_calendar{calendarNo}");
            if (!enabled)
                continue;

            var url = _displayService.GetConfig($"web_calendar_ics_url{calendarNo}");
            if (string.IsNullOrEmpty(url))
                continue;

            try
            {
                // TODO: Implement ICS calendar integration
                // var calendar = new ICalIntegration(url, _display, _minimalCacheExpiry);
                // todayEvents.AddRange(calendar.GetEventsBetween(date.Date, date.Date.AddDays(1).AddSeconds(-1)));
                // nearestEvents.AddRange(calendar.GetEventsBetween(date.Date, date.Date.AddMonths(12)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading calendar {CalendarNo}", calendarNo);
            }
        }

        todayEvents = todayEvents.OrderBy(e => e.StartTime).ToList();
        nearestEvents = nearestEvents.OrderBy(e => e.StartTime).ToList();
        var hasCalendarEntries = todayEvents.Any();

        // Group nearest events by date
        var nearestEventsGrouped = nearestEvents
            .GroupBy(e => e.StartTime.Date.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Generate icons using the PortalIconsService
        // FIXME var icons = _portalIconsService.GenerateIcons(date, hasCalendarEntries);

        return new CalendarInfo
        {
            TodayEvents = todayEvents,
            NearestEvents = nearestEvents,
            NearestEventsGrouped = nearestEventsGrouped,
            HasEntries = hasCalendarEntries
        };
    }
}

