using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

public class CalendarComponent : BaseComponent
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly CalendarContext _context;
    private readonly ILoggerFactory _loggerFactory;

    public CalendarComponent(
        ILogger<PageGeneratorService> logger,
        DisplayService displayService,
        DateTime date,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        CalendarContext context,
        ILoggerFactory loggerFactory)
        : base(logger, displayService, date)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _context = context;
        _loggerFactory = loggerFactory;
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
            var enabled = _displayService?.GetConfigBool($"web_calendar{calendarNo}") ?? false;
            if (!enabled)
                continue;

            var url = _displayService?.GetConfig($"web_calendar_ics_url{calendarNo}");
            if (string.IsNullOrEmpty(url))
                continue;

            try
            {
                // Create integration service for this calendar
                var calendarService = new IcalIntegrationService(
                    _loggerFactory.CreateLogger<IcalIntegrationService>(),
                    _httpClientFactory,
                    _memoryCache,
                    _context,
                    url,
                    _displayService?.GetCurrentDisplay());

                // Get today's events
                var today = _date.Date;
                var endOfDay = today.AddDays(1).AddSeconds(-1);
                var todayEventsData = calendarService.GetEventsBetweenAsync(today, endOfDay)
                    .GetAwaiter().GetResult();

                // Get events for the next 12 months
                var futureEventsData = calendarService.GetEventsBetweenAsync(today, today.AddMonths(12))
                    .GetAwaiter().GetResult();

                // Convert to CalendarEvent objects
                todayEvents.AddRange(todayEventsData.Select(ConvertToCalendarEvent));
                nearestEvents.AddRange(futureEventsData.Select(ConvertToCalendarEvent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading calendar {CalendarNo} from {Url}", calendarNo, url);
            }
        }

        todayEvents = todayEvents.OrderBy(e => e.StartTime).ToList();
        nearestEvents = nearestEvents.OrderBy(e => e.StartTime).ToList();
        var hasCalendarEntries = todayEvents.Any();

        // Group nearest events by date
        var nearestEventsGrouped = nearestEvents
            .GroupBy(e => e.StartTime.Date.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.ToList());

        return new CalendarInfo
        {
            TodayEvents = todayEvents,
            NearestEvents = nearestEvents,
            NearestEventsGrouped = nearestEventsGrouped,
            HasEntries = hasCalendarEntries
        };
    }

    /// <summary>
    /// Convert CalendarEventData to CalendarEvent
    /// </summary>
    private CalendarEvent ConvertToCalendarEvent(CalendarEventData data)
    {
        return new CalendarEvent
        {
            StartTime = data.StartTime,
            EndTime = data.EndTime,
            Summary = data.Summary,
            Description = data.Description,
            Location = data.Location,
            AllDay = data.IsAllDay,
            IsRecurrent = data.IsRecurrent,
            DurationHours = data.DurationHours
        };
    }
}

public class CalendarInfo
{
    public List<CalendarEvent> TodayEvents { get; set; } = new();
    public List<CalendarEvent> NearestEvents { get; set; } = new();
    public Dictionary<string, List<CalendarEvent>> NearestEventsGrouped { get; set; } = new();
    public bool HasEntries { get; set; }
}

public class CalendarEvent
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool AllDay { get; set; }
    public bool IsRecurrent { get; set; }
    public double? DurationHours { get; set; }
}
