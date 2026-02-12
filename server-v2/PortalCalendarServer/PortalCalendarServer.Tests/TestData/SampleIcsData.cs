namespace PortalCalendarServer.Tests.TestData;

/// <summary>
/// Sample ICS calendar data for testing
/// </summary>
public static class SampleIcsData
{
    /// <summary>
    /// Simple calendar with two events
    /// </summary>
    public static string SimpleCalendar => @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
X-WR-CALNAME:Test Calendar
X-WR-TIMEZONE:UTC
BEGIN:VEVENT
DTSTART:20240115T100000Z
DTEND:20240115T110000Z
DTSTAMP:20240101T000000Z
UID:event1@test.com
SUMMARY:Simple Event
DESCRIPTION:A simple test event
LOCATION:Test Location
STATUS:CONFIRMED
SEQUENCE:0
END:VEVENT
BEGIN:VEVENT
DTSTART:20240116T140000Z
DTEND:20240116T150000Z
DTSTAMP:20240101T000000Z
UID:event2@test.com
SUMMARY:Another Event
DESCRIPTION:Another test event
LOCATION:Another Location
STATUS:CONFIRMED
SEQUENCE:0
END:VEVENT
END:VCALENDAR";

    /// <summary>
    /// Calendar with an all-day event
    /// </summary>
    public static string AllDayEventCalendar => @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART;VALUE=DATE:20240115
DTEND;VALUE=DATE:20240116
DTSTAMP:20240101T000000Z
UID:allday1@test.com
SUMMARY:All Day Event
DESCRIPTION:An all-day test event
STATUS:CONFIRMED
SEQUENCE:0
END:VEVENT
END:VCALENDAR";

    /// <summary>
    /// Calendar with a recurring event
    /// </summary>
    public static string RecurringEventCalendar => @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20240115T100000Z
DTEND:20240115T110000Z
DTSTAMP:20240101T000000Z
UID:recurring1@test.com
RRULE:FREQ=DAILY;COUNT=5
SUMMARY:Recurring Event
DESCRIPTION:A recurring test event
STATUS:CONFIRMED
SEQUENCE:0
END:VEVENT
END:VCALENDAR";

    /// <summary>
    /// Calendar with multiple events on the same day
    /// </summary>
    public static string MultipleSameDayEventsCalendar => @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20240115T090000Z
DTEND:20240115T100000Z
DTSTAMP:20240101T000000Z
UID:morning@test.com
SUMMARY:Morning Event
DESCRIPTION:Morning meeting
STATUS:CONFIRMED
SEQUENCE:0
END:VEVENT
BEGIN:VEVENT
DTSTART:20240115T140000Z
DTEND:20240115T150000Z
DTSTAMP:20240101T000000Z
UID:afternoon@test.com
SUMMARY:Afternoon Event
DESCRIPTION:Afternoon meeting
STATUS:CONFIRMED
SEQUENCE:0
END:VEVENT
BEGIN:VEVENT
DTSTART:20240115T180000Z
DTEND:20240115T190000Z
DTSTAMP:20240101T000000Z
UID:evening@test.com
SUMMARY:Evening Event
DESCRIPTION:Evening meeting
STATUS:CONFIRMED
SEQUENCE:0
END:VEVENT
END:VCALENDAR";

    /// <summary>
    /// Calendar with special characters in summary
    /// </summary>
    public static string SpecialCharactersCalendar => @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20240115T100000Z
DTEND:20240115T110000Z
DTSTAMP:20240101T000000Z
UID:special@test.com
SUMMARY:Meeting with John\, Jane & Bob
DESCRIPTION:Important meeting\nnewline test
LOCATION:Room A\, Building B
STATUS:CONFIRMED
SEQUENCE:0
END:VEVENT
END:VCALENDAR";

    /// <summary>
    /// Empty calendar
    /// </summary>
    public static string EmptyCalendar => @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
END:VCALENDAR";

    /// <summary>
    /// Calendar with events spanning multiple days
    /// </summary>
    public static string MultiDayEventCalendar => @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20240115T000000Z
DTEND:20240117T000000Z
DTSTAMP:20240101T000000Z
UID:multiday@test.com
SUMMARY:Multi-day Event
DESCRIPTION:Event spanning multiple days
STATUS:CONFIRMED
SEQUENCE:0
END:VEVENT
END:VCALENDAR";
}
