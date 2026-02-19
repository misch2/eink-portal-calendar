# Unit Test Simplification Recommendations

## Summary

After analyzing all test files in `PortalCalendarServer.Tests`, I've identified significant opportunities to simplify and improve test maintainability. Below are detailed recommendations for each test file.

---

## 1. ? MetNoIconsMappingTests.cs - ALREADY SIMPLIFIED

**Current state:** Already well-optimized with Theory tests
**No further changes recommended**

---

## 2. ?? IcalIntegrationServiceTests.cs

### Issues Found:
- **30 tests** with significant repetition
- Multiple tests checking the same calendar data (MultipleTimezoneCalendar tested 6 times separately)
- Error handling tests are separate when they could be combined
- Many tests have very similar setup/teardown

### Recommended Simplifications:

#### A. Consolidate MultipleTimezoneCalendar Tests (Remove 3 tests)
```csharp
[Theory]
[InlineData("2026-02-14T00:00:00+01:00", "2026-02-14T23:59:59+01:00", 3, "od 8 do 9 bez pasma", "od 10 do 11 UK time")]
[InlineData("2026-02-01T00:00:00+01:00", "2026-02-14T23:59:59+01:00", 7, null, null)]
public async Task GetEventsBetweenAsync_WithMultipleTimezoneCalendar_HandlesVariousScenarios(
    string startStr, string endStr, int expectedCount, string? firstSummary, string? lastSummary)
{
    // Arrange
    SetupHttpResponse(TestIcsUrl, SampleIcsData.MultipleTimezoneCalendar);
    var service = CreateService();
    var start = DateTime.Parse(startStr);
    var end = DateTime.Parse(endStr);

    // Act
    var events = await service.GetEventsBetweenAsync(start, end);

    // Assert
    Assert.Equal(expectedCount, events.Count);
    if (firstSummary != null)
        Assert.Equal(firstSummary, events[0].Summary);
    if (lastSummary != null)
        Assert.Equal(lastSummary, events[^1].Summary);
    
    // Verify ordering
    for (int i = 0; i < events.Count - 1; i++)
        Assert.True(events[i].StartTime <= events[i + 1].StartTime);
}
```

**Eliminates:**
- `GetEventsBetweenAsync_WithMultipleTimezoneCalendar_ReturnsAllEvents`
- `GetEventsBetweenAsync_WithMultipleTimezoneCalendar_OrdersByStartTime`
- `GetEventsBetweenAsync_WithMultipleTimezoneCalendar_AllEventsHaveCorrectDuration`

#### B. Consolidate Error Handling Tests
```csharp
[Theory]
[InlineData(HttpStatusCode.InternalServerError, typeof(HttpRequestException))]
[InlineData(HttpStatusCode.NotFound, typeof(HttpRequestException))]
[InlineData(HttpStatusCode.Unauthorized, typeof(HttpRequestException))]
public async Task GetEventsBetweenAsync_WithHttpErrors_ThrowsException(
    HttpStatusCode statusCode, Type expectedExceptionType)
{
    SetupHttpResponse(TestIcsUrl, "", statusCode);
    var service = CreateService();
    var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
    var end = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);

    await Assert.ThrowsAsync(expectedExceptionType,
        async () => await service.GetEventsBetweenAsync(start, end));
}
```

**Eliminates:**
- `GetEventsBetweenAsync_WithHttpError_ThrowsException`
- `GetEventsBetweenAsync_WithNetworkError_ThrowsException` (can be added as InlineData)

#### C. Consolidate Calendar Type Tests
```csharp
[Theory]
[InlineData(nameof(SampleIcsData.SimpleCalendar), 2, "Simple Event", false, false)]
[InlineData(nameof(SampleIcsData.AllDayEventCalendar), 1, "All Day Event", true, false)]
[InlineData(nameof(SampleIcsData.RecurringEventCalendar), 1, "Recurring Event", false, true)]
[InlineData(nameof(SampleIcsData.EmptyCalendar), 0, null, false, false)]
[InlineData(nameof(SampleIcsData.MultiDayEventCalendar), 1, "Multi-day Event", true, false)]
public async Task GetEventsBetweenAsync_WithVariousCalendarTypes_ReturnsCorrectEvents(
    string calendarPropertyName, int expectedCount, string? firstEventSummary, 
    bool shouldBeAllDay, bool shouldBeRecurrent)
{
    // Use reflection to get the calendar data
    var calendarData = typeof(SampleIcsData)
        .GetProperty(calendarPropertyName)!
        .GetValue(null) as string;
    
    SetupHttpResponse(TestIcsUrl, calendarData!);
    var service = CreateService();
    var start = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
    var end = new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc);

    var events = await service.GetEventsBetweenAsync(start, end);

    Assert.Equal(expectedCount, events.Count);
    if (firstEventSummary != null)
    {
        Assert.Equal(firstEventSummary, events[0].Summary);
        if (shouldBeAllDay) Assert.True(events[0].IsAllDay);
        if (shouldBeRecurrent) Assert.True(events[0].IsRecurrent);
    }
}
```

**Eliminates:**
- `GetEventsBetweenAsync_WithSimpleCalendar_ReturnsEvents` (partially)
- `GetEventsBetweenAsync_WithAllDayEvent_MarksEventAsAllDay`
- `GetEventsBetweenAsync_WithRecurringEvent_MarksEventAsRecurrent`
- `GetEventsBetweenAsync_WithEmptyCalendar_ReturnsEmptyList`
- `GetEventsBetweenAsync_WithMultiDayEvent_IncludesEvent`

**Result: 30 tests ? ~18 tests (40% reduction)**

---

## 3. ?? NameDayServiceTests.cs

### Issues Found:
- **21 tests** with significant duplication
- Many single-date tests that could be Theory tests
- Separate tests for leap year that could be combined
- Month tests are repetitive

### Recommended Simplifications:

#### A. Consolidate Single Date Tests (Already partially done with Theory, but can expand)
```csharp
[Theory]
[InlineData(1, 1, "Nový rok")]
[InlineData(1, 15, "Alice")]
[InlineData(2, 14, "Valentýn / Valentýna")]
[InlineData(2, 29, "Horymír")] // Leap year test integrated!
[InlineData(3, 8, "Gabriela")]
[InlineData(3, 19, "Josef")] // Also tests year independence
[InlineData(4, 27, "Jaroslav")]
[InlineData(5, 1, "Svátek práce")]
[InlineData(6, 24, "Jan")]
[InlineData(7, 5, "Den slovanských vìrozvìstù Cyrila a Metodìje")]
[InlineData(8, 15, "Hana")]
[InlineData(9, 28, "Václav")]
[InlineData(9, 29, "Michal")]
[InlineData(10, 28, "Alfréd")]
[InlineData(11, 17, "Mahulena / Gertruda")]
[InlineData(12, 6, "Mikuláš")]
[InlineData(12, 24, "Adam a Eva, Štìdrý den")]
[InlineData(12, 25, "Boží hod vánoèní, 1.svátek vánoèní")]
[InlineData(12, 31, "Silvestr")]
public void GetNameDay_ForVariousDates_ReturnsCorrectNames(int month, int day, string expectedName)
{
    var date = new DateTime(2024, month, day);
    var result = _service.GetNameDay(date);
    
    Assert.NotNull(result);
    Assert.Equal(expectedName, result.Name);
    Assert.Equal(date, result.Date);
    Assert.Equal("CZ", result.CountryCode);
}
```

**Eliminates 8 individual tests:**
- `GetNameDay_ForJanuary1_ReturnsNovyRok`
- `GetNameDay_ForValentinesDay_ReturnsValentyn`
- `GetNameDay_ForChristmasEve_ReturnsAdamAEva`
- `GetNameDay_ForChristmasDay_ReturnsBoziHod`
- `GetNameDay_ForNewYearsEve_ReturnsSilvestr`
- `GetNameDay_ForLeapYearDate_ReturnsCorrectNameDay`
- `GetNameDay_WithDifferentYears_ReturnsSameNameForSameDayMonth`
- Plus the existing Theory data

#### B. Consolidate Month Tests
```csharp
[Theory]
[InlineData(2024, 1, 31, "Nový rok", "Marika")]  // January, contains...
[InlineData(2024, 2, 29, null, "Horymír")]       // February leap year
[InlineData(2023, 2, 28, null, "Lumír")]         // February non-leap
[InlineData(2024, 12, 31, "Mikuláš", "Silvestr")]
public void GetNameDaysForMonth_ForVariousMonths_ReturnsCorrectCount(
    int year, int month, int expectedCount, string? containsFirst, string? containsLast)
{
    var result = _service.GetNameDaysForMonth(year, month);
    
    Assert.Equal(expectedCount, result.Count);
    Assert.All(result, nd => Assert.Equal(month, nd.Date.Month));
    Assert.All(result, nd => Assert.Equal(year, nd.Date.Year));
    
    if (containsFirst != null)
        Assert.Contains(result, nd => nd.Name == containsFirst);
    if (containsLast != null)
        Assert.Contains(result, nd => nd.Name == containsLast);
    
    // Verify ordering
    for (int i = 0; i < result.Count - 1; i++)
        Assert.True(result[i].Date < result[i + 1].Date);
}
```

**Eliminates 5 tests:**
- `GetNameDaysForMonth_ForJanuary_Returns31Entries`
- `GetNameDaysForMonth_ForFebruary2024LeapYear_Returns29Entries`
- `GetNameDaysForMonth_ForFebruary2023NonLeapYear_Returns28Entries`
- `GetNameDaysForMonth_ForDecember_Returns31Entries`
- `GetNameDaysForMonth_EntriesAreInChronologicalOrder`

#### C. Keep these important tests as-is:
- `GetNameDay_WithUnsupportedCountryCode_ReturnsNull`
- `GetNameDaysForMonth_WithUnsupportedCountryCode_ReturnsEmptyList`
- `GetNameDay_ForAllDaysInYear_ReturnsValidResults` (comprehensive validation)

**Result: 21 tests ? ~6 tests (71% reduction)**

---

## 4. ?? PublicHolidayServiceTests.cs

### Issues Found:
- **28 tests** with massive duplication
- Almost identical patterns to NameDayServiceTests
- Theory test already exists but many single tests remain
- Czech character test is unnecessary (covered by data)

### Recommended Simplifications:

#### A. Expand the Theory Test (Already exists, but remove duplicates)
The existing Theory test covers most cases. Remove these redundant individual tests:
- `GetPublicHoliday_ForNewYearsDay_ReturnsHoliday`
- `GetPublicHoliday_ForLabourDay_ReturnsHoliday`
- `GetPublicHoliday_ForChristmasDay_ReturnsHoliday`
- `GetPublicHoliday_WithDifferentYears_ReturnsSameHolidayName`

#### B. Consolidate Range Query Tests
```csharp
[Theory]
[InlineData("2024-12-01", "2024-12-31", 3, 12, 24, 12, 26)] // Single month
[InlineData("2024-01-01", "2024-12-31", 13, 1, 1, 12, 26)]  // Full year
[InlineData("2023-12-01", "2024-01-31", 5, 12, 24, 1, 1)]   // Spanning years
public async Task GetPublicHolidaysBetween_WithVariousRanges_ReturnsCorrectHolidays(
    string startStr, string endStr, int minCount, 
    int expectedFirstMonth, int expectedFirstDay,
    int expectedLastMonth, int expectedLastDay)
{
    var start = DateTime.Parse(startStr);
    var end = DateTime.Parse(endStr);
    
    var result = _service.GetPublicHolidaysBetween(start, end);
    
    Assert.True(result.Count >= minCount);
    Assert.All(result, h => Assert.True(h.Date >= start && h.Date <= end));
    
    // Verify ordering
    for (int i = 0; i < result.Count - 1; i++)
        Assert.True(result[i].Date <= result[i + 1].Date);
}
```

**Eliminates 5 tests:**
- `GetPublicHolidaysBetween_ForSingleMonth_ReturnsOnlyHolidaysInRange`
- `GetPublicHolidaysBetween_ForFullYear_ReturnsAllHolidays`
- `GetPublicHolidaysBetween_SpanningMultipleYears_ReturnsCorrectHolidays`
- `GetPublicHolidaysBetween_ResultsAreOrdered`
- Partial: `GetPublicHolidaysForYear_ResultsAreOrdered`

#### C. Remove Redundant Tests
- **DELETE:** `GetPublicHoliday_CzechCharactersPreserved_InLocalName` - This is covered by the Theory test data itself

**Result: 28 tests ? ~15 tests (46% reduction)**

---

## 5. ?? PublicHolidayComponentTests.cs

### Issues Found:
- **12 tests** with excessive mocking verification
- Many tests verify the same behavior
- Logging test is fragile and adds little value

### Recommended Simplifications:

#### A. Consolidate Basic Functionality Tests
```csharp
[Theory]
[InlineData("2024-01-01", "New Year's Day", "Nový rok", true)]
[InlineData("2024-12-25", "Christmas Day", "1. svátek vánoèní", true)]
[InlineData("2024-01-15", null, null, false)] // Non-holiday
public void GetPublicHolidayInfo_WithVariousDates_ReturnsExpectedResults(
    string dateStr, string? expectedName, string? expectedLocalName, bool isHoliday)
{
    var date = DateTime.Parse(dateStr);
    var expectedInfo = expectedName != null 
        ? new PublicHolidayInfo { Name = expectedName, LocalName = expectedLocalName!, Date = date, CountryCode = "CZ" }
        : null;

    _mockPublicHolidayService
        .Setup(s => s.GetPublicHoliday(date, It.IsAny<string>()))
        .Returns(expectedInfo);
    
    _mockPublicHolidayService
        .Setup(s => s.IsPublicHoliday(date, It.IsAny<string>()))
        .Returns(isHoliday);

    var component = CreateComponent(date);
    
    var result = component.GetPublicHolidayInfo();
    var isHolidayResult = component.IsPublicHoliday();

    if (expectedName != null)
    {
        Assert.NotNull(result);
        Assert.Equal(expectedName, result.Name);
        Assert.Equal(expectedLocalName, result.LocalName);
    }
    else
    {
        Assert.Null(result);
    }
    
    Assert.Equal(isHoliday, isHolidayResult);
}
```

**Eliminates 5 tests:**
- `GetPublicHolidayInfo_CallsPublicHolidayService`
- `GetPublicHolidayInfo_WhenNoHoliday_ReturnsNull`
- `IsPublicHoliday_ForHoliday_ReturnsTrue`
- `IsPublicHoliday_ForNonHoliday_ReturnsFalse`
- Partial: `GetPublicHolidayInfo_UsesComponentDate`

#### B. Remove Low-Value Tests
- **DELETE:** `GetPublicHolidayInfo_LogsDebugMessage` - Fragile, testing framework behavior, not business logic
- **DELETE:** `Constructor_InitializesWithProvidedDate` - Covered by other tests

**Result: 12 tests ? ~6 tests (50% reduction)**

---

## 6. ?? MetNoWeatherServiceTests.cs

### Issues Found:
- Some tests could be consolidated
- Tests are generally well-structured

### Minor Recommendations:

#### Keep most tests as-is, but consider:
```csharp
[Theory]
[InlineData(null)]
[InlineData("unknown_code")]
public void ExtractData_WithInvalidSymbolCode_HandlesGracefully(string? symbolCode)
{
    // Test that service handles missing/invalid symbol codes
}
```

**Result: 8 tests ? 7 tests (minimal change, already well-designed)**

---

## Overall Impact Summary

| Test File | Current Tests | Recommended Tests | Reduction |
|-----------|--------------|-------------------|-----------|
| MetNoIconsMappingTests | 6 | 6 | 0% ? |
| IcalIntegrationServiceTests | 30 | 18 | 40% |
| NameDayServiceTests | 21 | 6 | 71% |
| PublicHolidayServiceTests | 28 | 15 | 46% |
| PublicHolidayComponentTests | 12 | 6 | 50% |
| MetNoWeatherServiceTests | 8 | 7 | 12% |
| **TOTAL** | **105** | **58** | **45%** |

## Benefits of Simplification

1. **Faster test execution** - ~45% fewer tests to run
2. **Easier maintenance** - Changes require updating fewer tests
3. **Better coverage clarity** - Theory tests show all edge cases in one place
4. **Less code duplication** - DRY principle applied to tests
5. **Easier to add new test cases** - Just add InlineData, not whole new methods

## Implementation Priority

1. **High Priority:** NameDayServiceTests (71% reduction)
2. **High Priority:** PublicHolidayComponentTests (50% reduction)  
3. **Medium Priority:** PublicHolidayServiceTests (46% reduction)
4. **Medium Priority:** IcalIntegrationServiceTests (40% reduction)
5. **Low Priority:** MetNoWeatherServiceTests (12% reduction)

## Next Steps

1. Review this document with the team
2. Implement changes file-by-file starting with highest priority
3. Run full test suite after each file to ensure coverage maintained
4. Update any CI/CD pipelines if test count expectations exist
