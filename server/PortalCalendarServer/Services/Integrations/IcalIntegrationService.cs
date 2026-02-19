using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Integration service for fetching and parsing ICS (iCalendar) calendars.
/// Based on PortalCalendar::Integration::iCal from Perl.
/// </summary>
public class IcalIntegrationService : IntegrationServiceBase
{
    private readonly string _icsUrl;

    public IcalIntegrationService(
        ILogger<IcalIntegrationService> logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        IDatabaseCacheServiceFactory databaseCacheFactory,
        CalendarContext context,
        string icsUrl)
        : base(logger, httpClientFactory, memoryCache, databaseCacheFactory, context)
    {
        _icsUrl = icsUrl;
    }

    public override bool IsConfigured(Display display)
    {
        return !string.IsNullOrWhiteSpace(_icsUrl);
    }

    /// <summary>
    /// Fetch raw ICS data from the web
    /// </summary>
    private async Task<string> GetRawDetailsFromWebAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching ICS data from {Url}", _icsUrl);

        var response = await httpClient.GetAsync(_icsUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Get cached raw ICS data from web (2 hour cache)
    /// </summary>
    private async Task<string> GetCachedRawDetailsFromWebAsync(CancellationToken cancellationToken = default)
    {
        var dbCacheService = databaseCacheFactory.Create(nameof(IcalIntegrationService), TimeSpan.FromHours(2));

        var content = await dbCacheService.GetOrSetAsync(
            async () => await GetRawDetailsFromWebAsync(cancellationToken),
            new { url = _icsUrl },
            cancellationToken);

        return content;
    }

    /// <summary>
    /// Parse ICS content and extract events within the specified date range
    /// </summary>
    private List<CalendarEventData> ParseCalendarEvents(string icsContent, DateTime start, DateTime end)
    {
        logger.LogDebug("Parsing calendar data for range {Start} to {End}", start, end);

        var events = new List<CalendarEventData>();

        try
        {
            var calendar = Calendar.Load(icsContent);
            if (calendar == null)
            {
                throw new Exception("Failed to parse calendar, no data produced");
            }

            var calStart = new CalDateTime(start);
            var calEnd = new CalDateTime(end);
            var allOccurrences = calendar.GetOccurrences(calStart).TakeWhileBefore(calEnd).ToList();

            var calEvents = calendar.Events;

            logger.LogInformation("Calendar has {Count} total occurrences between {Start} and {End}", allOccurrences.Count, start, end);

            foreach (var occurrence in allOccurrences)
            {
                var calEvent = (CalendarEvent)occurrence.Source;
                try
                {
                    //logger.LogDebug("Occurrence: {Summary} at {Start} to {End}", calEvent.Summary, occurrence.Period.StartTime, occurrence.Period.EndTime);

                    if (calEvent.Start == null || calEvent.End == null)
                    {
                        logger.LogWarning("Event {Uid} has invalid start or end time: {Summary}", calEvent.Uid, calEvent.Summary);
                        continue;
                    }

                    // Check if event falls within date range
                    var eventStart = occurrence.Period.StartTime.AsUtc;
                    var eventEnd = occurrence.Period.EffectiveEndTime.AsUtc;

                    // Skip events outside the range
                    if (eventEnd < start || eventStart > end)
                        continue;

                    // Determine if it's an all-day event
                    var isAllDay = calEvent.IsAllDay ||
                                   (eventEnd - eventStart).TotalDays >= 1 &&
                                   eventStart.TimeOfDay == TimeSpan.Zero;

                    // Calculate duration in hours
                    double? durationHours = null;
                    if (!isAllDay && eventEnd > eventStart)
                    {
                        durationHours = (eventEnd - eventStart).TotalHours;
                    }

                    // Check if it's a recurring event
                    var isRecurrent = calEvent.RecurrenceRules != null && calEvent.RecurrenceRules.Count > 0;

                    var eventData = new CalendarEventData
                    {
                        Uid = calEvent.Uid ?? string.Empty,
                        Summary = calEvent.Summary ?? string.Empty,
                        Description = calEvent.Description ?? string.Empty,
                        Location = calEvent.Location ?? string.Empty,
                        StartTime = eventStart,
                        EndTime = eventEnd,
                        IsAllDay = isAllDay,
                        DurationHours = durationHours,
                        IsRecurrent = isRecurrent
                    };

                    events.Add(eventData);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error processing event {Uid}: {Summary}", calEvent.Uid, calEvent.Summary);
                }
            }

            logger.LogDebug("Parsed {Count} events from calendar", events.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing calendar data from {Url}", _icsUrl);
        }

        return events;
    }

    /// <summary>
    /// Get all calendar events within the specified date range
    /// </summary>
    public async Task<List<CalendarEventData>> GetEventsBetweenAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"ical:events:{_icsUrl}:{start:yyyyMMddHHmmss}:{end:yyyyMMddHHmmss}";
        var cacheExpiration = TimeSpan.FromHours(2);

        // Try to get from memory cache
        if (memoryCache.TryGetValue<List<CalendarEventData>>(cacheKey, out var cachedEvents) && cachedEvents != null)
        {
            logger.LogDebug("ICS events cache HIT for {Url}", _icsUrl);
            return cachedEvents;
        }

        logger.LogDebug("ICS events cache MISS for {Url}", _icsUrl);

        // Get raw ICS content
        var icsContent = await GetCachedRawDetailsFromWebAsync(cancellationToken);

        // For all-day events, we need to start from the beginning of the day
        var searchStart = start.Date;

        // Parse events
        var events = ParseCalendarEvents(icsContent, searchStart, end);

        // Filter events manually to handle all-day events correctly
        // All-day events should be included even if start time is after the filter start
        var filteredEvents = events
            .Where(e => e.StartTime > start || e.IsAllDay)
            .OrderBy(e => e.StartTime)
            .ToList();

        // Cache the result
        memoryCache.Set(cacheKey, filteredEvents, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheExpiration,
            Size = filteredEvents.Count * 1024 // Rough estimate
        });

        return filteredEvents;
    }

    /// <summary>
    /// Get events for today
    /// </summary>
    public async Task<List<CalendarEventData>> GetTodayEventsAsync(
        DateTime today,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = today.Date;
        var endOfDay = today.Date.AddDays(1).AddSeconds(-1);

        return await GetEventsBetweenAsync(startOfDay, endOfDay, cancellationToken);
    }

    /// <summary>
    /// Get events for the next N days
    /// </summary>
    public async Task<List<CalendarEventData>> GetUpcomingEventsAsync(
        DateTime from,
        int daysAhead,
        CancellationToken cancellationToken = default)
    {
        var start = from.Date;
        var end = start.AddDays(daysAhead);

        return await GetEventsBetweenAsync(start, end, cancellationToken);
    }
}

/// <summary>
/// Calendar event data extracted from ICS
/// </summary>
public class CalendarEventData
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Uid { get; set; } = string.Empty;

    /// <summary>
    /// Event summary/title
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Event description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Event location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Event start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Event end time
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Whether this is an all-day event
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    /// Duration in hours (null for all-day events)
    /// </summary>
    public double? DurationHours { get; set; }

    /// <summary>
    /// Whether this event is part of a recurring series
    /// </summary>
    public bool IsRecurrent { get; set; }
}
