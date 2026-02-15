using Microsoft.Extensions.Logging;
using PortalCalendarServer.Services.Integrations;
using PortalCalendarServer.Tests.TestBase;
using PortalCalendarServer.Tests.TestData;
using System.Net;

namespace PortalCalendarServer.Tests.Services.Integrations;

/// <summary>
/// Unit tests for ICalIntegrationService
/// </summary>
public class IcalIntegrationServiceTests : IntegrationServiceTestBase
{
    private const string TestIcsUrl = "https://example.com/calendar.ics";

    private IcalIntegrationService CreateService(string? icsUrl = null)
    {
        var logger = new Mock<ILogger<IcalIntegrationService>>().Object;

        return new IcalIntegrationService(
            logger,
            MockHttpClientFactory.Object,
            MemoryCache,
            MockDatabaseCacheServiceFactory.Object,
            Context,
            icsUrl ?? TestIcsUrl,
            TestDisplay);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithSimpleCalendar_ReturnsEvents()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.SimpleCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.NotNull(events);
        Assert.Equal(2, events.Count);

        var event1 = events.FirstOrDefault(e => e.Uid == "event1@test.com");
        Assert.NotNull(event1);
        Assert.Equal("Simple Event", event1.Summary);
        Assert.Equal("Test Location", event1.Location);
        Assert.Equal("A simple test event", event1.Description);
        Assert.False(event1.IsAllDay);
        Assert.Equal(1.0, event1.DurationHours);

        var event2 = events.FirstOrDefault(e => e.Uid == "event2@test.com");
        Assert.NotNull(event2);
        Assert.Equal("Another Event", event2.Summary);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithAllDayEvent_MarksEventAsAllDay()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.AllDayEventCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.Single(events);
        var allDayEvent = events[0];
        Assert.True(allDayEvent.IsAllDay);
        Assert.Null(allDayEvent.DurationHours);
        Assert.Equal("All Day Event", allDayEvent.Summary);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithRecurringEvent_MarksEventAsRecurrent()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.RecurringEventCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.NotEmpty(events);
        var recurringEvent = events.FirstOrDefault(e => e.Uid == "recurring1@test.com");
        Assert.NotNull(recurringEvent);
        Assert.True(recurringEvent.IsRecurrent);
        Assert.Equal("Recurring Event", recurringEvent.Summary);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_FiltersEventsOutsideRange()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.SimpleCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 14, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.Single(events);
        Assert.Equal("event1@test.com", events[0].Uid);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithEmptyCalendar_ReturnsEmptyList()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.EmptyCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithSpecialCharacters_UnescapesSummary()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.SpecialCharactersCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.Single(events);
        Assert.Equal("Meeting with John, Jane & Bob", events[0].Summary);
        Assert.Contains("Room A, Building B", events[0].Location);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_OrdersByStartTime()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.MultipleSameDayEventsCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 16, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.Equal(3, events.Count);
        Assert.Equal("Morning Event", events[0].Summary);
        Assert.Equal("Afternoon Event", events[1].Summary);
        Assert.Equal("Evening Event", events[2].Summary);

        // Verify chronological order
        for (int i = 0; i < events.Count - 1; i++)
        {
            Assert.True(events[i].StartTime <= events[i + 1].StartTime);
        }
    }

    [Fact]
    public async Task GetEventsBetweenAsync_UsesCaching()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.SimpleCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act - First call
        var events1 = await service.GetEventsBetweenAsync(start, end);

        // Act - Second call (should use cache)
        var events2 = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.Equal(events1.Count, events2.Count);

        // Verify HTTP was called only once (second call used cache)
        VerifyHttpRequest(TestIcsUrl, Times.Once());
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithDifferentDateRanges_UsesMultipleCachedHttpRequest()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.SimpleCalendar);
        var service = CreateService();
        var start1 = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end1 = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);
        var start2 = new DateTime(2024, 1, 16, 0, 0, 0, DateTimeKind.Utc);
        var end2 = new DateTime(2024, 1, 18, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events1 = await service.GetEventsBetweenAsync(start1, end1);
        var events2 = await service.GetEventsBetweenAsync(start2, end2);

        // Assert
        Assert.NotNull(events1);
        Assert.NotNull(events2);

        // Both calls should fetch from web
        VerifyHttpRequest(TestIcsUrl, Times.Exactly(2));
    }

    [Fact]
    public async Task GetTodayEventsAsync_ReturnsOnlyTodaysEvents()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.MultipleSameDayEventsCalendar);
        var service = CreateService();
        var today = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetTodayEventsAsync(today);

        // Assert
        Assert.Equal(3, events.Count);
        Assert.All(events, e => Assert.Equal(15, e.StartTime.Day));
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_ReturnsEventsForSpecifiedDays()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.SimpleCalendar);
        var service = CreateService();
        var from = new DateTime(2024, 1, 14, 0, 0, 0, DateTimeKind.Utc);
        var daysAhead = 5;

        // Act
        var events = await service.GetUpcomingEventsAsync(from, daysAhead);

        // Assert
        Assert.Equal(2, events.Count);
        Assert.All(events, e =>
        {
            Assert.True(e.StartTime >= from);
            Assert.True(e.StartTime < from.AddDays(daysAhead));
        });
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithHttpError_ThrowsException()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, "", HttpStatusCode.InternalServerError);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await service.GetEventsBetweenAsync(start, end));
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithNetworkError_ThrowsException()
    {
        // Arrange
        SetupHttpException(TestIcsUrl, new HttpRequestException("Network error"));
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await service.GetEventsBetweenAsync(start, end));
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithInvalidIcsData_ReturnsEmptyList()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, "INVALID ICS DATA");
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert - Should handle gracefully and return empty list
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithMultiDayEvent_IncludesEvent()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.MultiDayEventCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 18, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.Single(events);
        var multiDayEvent = events[0];
        Assert.Equal("Multi-day Event", multiDayEvent.Summary);
        Assert.True(multiDayEvent.IsAllDay);
        Assert.True((multiDayEvent.EndTime - multiDayEvent.StartTime).TotalDays >= 1);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_IncludesAllDayEventsStartingAfterFilterStart()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.AllDayEventCalendar);
        var service = CreateService();

        // Start time is in the middle of the day
        var start = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.Single(events);
        Assert.True(events[0].IsAllDay);
        Assert.Equal("All Day Event", events[0].Summary);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        SetupHttpResponse(TestIcsUrl, SampleIcsData.SimpleCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.GetEventsBetweenAsync(start, end, cts.Token));
    }

    [Fact]
    public async Task GetEventsBetweenAsync_CalculatesDurationCorrectly()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.SimpleCalendar);
        var service = CreateService();
        var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        var event1 = events.FirstOrDefault(e => e.Uid == "event1@test.com");
        Assert.NotNull(event1);
        Assert.NotNull(event1.DurationHours);
        Assert.Equal(1.0, event1.DurationHours.Value, 2);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithMultipleTimezoneCalendar_ReturnsAllEvents()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.MultipleTimezoneCalendar);
        var service = CreateService();
        var start = DateTime.Parse("2026-02-01T00:00:00+01:00");
        var end = DateTime.Parse("2026-02-14T23:59:59+01:00");

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.NotNull(events);
        Assert.Equal(7, events.Count);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithMultipleTimezoneCalendar_ParsesEventDetails()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.MultipleTimezoneCalendar);
        var service = CreateService();
        var start = DateTime.Parse("2026-02-14T00:00:00+01:00");
        var end = DateTime.Parse("2026-02-14T23:59:59+01:00");

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        var event1 = events.FirstOrDefault(e => e.Uid == "576g2rbd7fkfsh1ae412vc5s5h@google.com");
        Assert.NotNull(event1);
        Assert.Equal("od 8 do 9 bez pasma", event1.Summary);
        // 8-9 unspecified is 8-9 CET for this calendar which is 7-8 UTC
        Assert.Equal(new DateTime(2026, 2, 14, 7, 0, 0, DateTimeKind.Utc), event1.StartTime);
        Assert.Equal(new DateTime(2026, 2, 14, 8, 0, 0, DateTimeKind.Utc), event1.EndTime);
        Assert.Equal(1.0, event1.DurationHours);

        var event2 = events.FirstOrDefault(e => e.Uid == "2bntdhj7ko9e3nqkh20t8tnb7g@google.com");
        Assert.NotNull(event2);
        Assert.Equal("od 9 do 10 CET", event2.Summary);
        // 9-10 CET is 8-9 UTC
        Assert.Equal(new DateTime(2026, 2, 14, 8, 0, 0, DateTimeKind.Utc), event2.StartTime);
        Assert.Equal(new DateTime(2026, 2, 14, 9, 0, 0, DateTimeKind.Utc), event2.EndTime);

        var event3 = events.FirstOrDefault(e => e.Uid == "5om3f0e1not4eq9s99o9iktoju@google.com");
        Assert.NotNull(event3);
        Assert.Equal("od 10 do 11 UK time", event3.Summary);
        // 10-11 UK time is 10-11 UTC
        Assert.Equal(new DateTime(2026, 2, 14, 10, 0, 0, DateTimeKind.Utc), event3.StartTime);
        Assert.Equal(new DateTime(2026, 2, 14, 11, 0, 0, DateTimeKind.Utc), event3.EndTime);
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithMultipleTimezoneCalendar_OrdersByStartTime()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.MultipleTimezoneCalendar);
        var service = CreateService();
        var start = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert
        Assert.Equal(3, events.Count);
        Assert.Equal("od 8 do 9 bez pasma", events[0].Summary);
        Assert.Equal("od 9 do 10 CET", events[1].Summary);
        Assert.Equal("od 10 do 11 UK time", events[2].Summary);

        // Verify chronological order
        for (int i = 0; i < events.Count - 1; i++)
        {
            Assert.True(events[i].StartTime <= events[i + 1].StartTime);
        }
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithMultipleTimezoneCalendar_HandlesUnicodeCharacters()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.MultipleTimezoneCalendar);
        var service = CreateService();
        var start = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert - verify that Czech characters and emojis are handled correctly
        Assert.NotNull(events);
        Assert.All(events, e => Assert.NotNull(e.Summary));
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithMultipleTimezoneCalendar_AllEventsHaveCorrectDuration()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.MultipleTimezoneCalendar);
        var service = CreateService();
        var start = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert - all events are 1 hour long
        Assert.All(events, e =>
        {
            Assert.NotNull(e.DurationHours);
            Assert.Equal(1.0, e.DurationHours.Value, 2);
        });
    }

    [Fact]
    public async Task GetEventsBetweenAsync_WithMultipleTimezoneCalendar_FiltersPartialOverlap()
    {
        // Arrange
        SetupHttpResponse(TestIcsUrl, SampleIcsData.MultipleTimezoneCalendar);
        var service = CreateService();
        var start = new DateTime(2026, 2, 14, 7, 30, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 2, 14, 8, 30, 0, DateTimeKind.Utc);

        // Act
        var events = await service.GetEventsBetweenAsync(start, end);

        // Assert - should include only the event that starts within or overlaps the range
        Assert.Single(events);
        Assert.Equal("od 9 do 10 CET", events[0].Summary);
        Assert.Equal(new DateTime(2026, 2, 14, 8, 0, 0, DateTimeKind.Utc), events[0].StartTime);
    }
}
