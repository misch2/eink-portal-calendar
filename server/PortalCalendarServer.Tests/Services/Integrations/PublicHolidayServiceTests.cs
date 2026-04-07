using Microsoft.Extensions.Logging;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Tests.Services.Integrations;

/// <summary>
/// Unit tests for PublicHolidayService
/// </summary>
public class PublicHolidayServiceTests
{
    private readonly Mock<ILogger<PublicHolidayService>> _mockLogger;
    private readonly PublicHolidayService _service;

    public PublicHolidayServiceTests()
    {
        _mockLogger = new Mock<ILogger<PublicHolidayService>>();
        _service = new PublicHolidayService(_mockLogger.Object);
    }

    [Fact]
    public void GetPublicHoliday_ForNewYearsDayInCZ_ReturnsHoliday()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);

        // Act
        var result = _service.GetPublicHoliday(date, "CZ");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Nový rok", result.Name);
        Assert.Equal(date.Date, result.Date.Date);
    }

    [Fact]
    public void GetPublicHoliday_ForLabourDayInCZ_ReturnsHolidayFor()
    {
        // Arrange
        var date = new DateTime(2024, 5, 1);

        // Act
        var result = _service.GetPublicHoliday(date, "CZ");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Svátek práce", result.Name);
        Assert.Equal(date.Date, result.Date.Date);
    }

    [Fact]
    public void GetPublicHoliday_ForNonHoliday_ReturnsNull()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15); // Regular Monday

        // Act
        var result = _service.GetPublicHoliday(date, "CZ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPublicHoliday_WithUnsupportedCountryCode_ReturnsNull()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);

        // Act
        var result = _service.GetPublicHoliday(date, "US");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPublicHoliday_WithDifferentYears_ReturnsSameHolidayName()
    {
        // Arrange
        var date2024 = new DateTime(2024, 1, 1);
        var date2025 = new DateTime(2025, 1, 1);

        // Act
        var result2024 = _service.GetPublicHoliday(date2024, "CZ");
        var result2025 = _service.GetPublicHoliday(date2025, "CZ");

        // Assert
        Assert.NotNull(result2024);
        Assert.NotNull(result2025);
        Assert.Equal(result2024.Name, result2025.Name);
    }

    [Theory]
    [InlineData(1, 1, "Nový rok")]
    [InlineData(5, 1, "Svátek práce")]
    [InlineData(5, 8, "Den vítězství")]
    [InlineData(7, 5, "Den slovanských věrozvěstů Cyrila a Metoděje")]
    [InlineData(7, 6, "Den upálení mistra Jana Husa")]
    [InlineData(9, 28, "Den české státnosti")]
    [InlineData(10, 28, "Den vzniku samostatného československého státu")]
    [InlineData(11, 17, "Den boje za svobodu a demokracii")]
    [InlineData(12, 24, "Štědrý den")]
    [InlineData(12, 25, "1. svátek vánoční")]
    [InlineData(12, 26, "2. svátek vánoční")]
    public void GetPublicHoliday_ForVariousHolidays_ReturnsCorrectInfo(
        int month, int day, string expectedName)
    {
        // Arrange
        var date = new DateTime(2024, month, day);

        // Act
        var result = _service.GetPublicHoliday(date, "CZ");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedName, result.Name);
    }

    [Fact]
    public void GetPublicHolidaysForYear_For2024_ReturnsAllHolidays()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.GetPublicHolidaysForYear(year, "CZ");

        // Assert
        Assert.NotEmpty(result);
        // Czech Republic has 13 fixed public holidays + movable Easter holidays
        Assert.True(result.Count >= 13);
        Assert.All(result, h => Assert.Equal(year, h.Date.Year));
    }

    [Fact]
    public void GetPublicHolidaysForYear_ResultsAreOrdered()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.GetPublicHolidaysForYear(year, "CZ");

        // Assert
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].Date <= result[i + 1].Date);
        }
    }

    [Fact]
    public void GetPublicHolidaysForYear_WithUnsupportedCountryCode_ReturnsEmptyList()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.GetPublicHolidaysForYear(year, "US");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetPublicHolidaysBetween_ForSingleMonth_ReturnsOnlyHolidaysInRange()
    {
        // Arrange
        var startDate = new DateTime(2024, 12, 1);
        var endDate = new DateTime(2024, 12, 31);

        // Act
        var result = _service.GetPublicHolidaysBetween(startDate, endDate, "CZ");

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, h => Assert.True(h.Date >= startDate && h.Date <= endDate));
        Assert.Contains(result, h => h.Date.Month == 12 && h.Date.Day == 24); // Christmas Eve
        Assert.Contains(result, h => h.Date.Month == 12 && h.Date.Day == 25); // Christmas Day
        Assert.Contains(result, h => h.Date.Month == 12 && h.Date.Day == 26); // St. Stephen's Day
    }

    [Fact]
    public void GetPublicHolidaysBetween_ForFullYear_ReturnsAllHolidays()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        // Act
        var result = _service.GetPublicHolidaysBetween(startDate, endDate, "CZ");

        // Assert
        var yearHolidays = _service.GetPublicHolidaysForYear(2024, "CZ");
        Assert.Equal(yearHolidays.Count, result.Count);
    }

    [Fact]
    public void GetPublicHolidaysBetween_SpanningMultipleYears_ReturnsCorrectHolidays()
    {
        // Arrange
        var startDate = new DateTime(2023, 12, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var result = _service.GetPublicHolidaysBetween(startDate, endDate, "CZ");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, h => h.Date.Year == 2023 && h.Date.Month == 12);
        Assert.Contains(result, h => h.Date.Year == 2024 && h.Date.Month == 1);
        Assert.All(result, h => Assert.True(h.Date >= startDate && h.Date <= endDate));
    }

    [Fact]
    public void GetPublicHolidaysBetween_ResultsAreOrdered()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        // Act
        var result = _service.GetPublicHolidaysBetween(startDate, endDate, "CZ");

        // Assert
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].Date <= result[i + 1].Date);
        }
    }

    [Fact]
    public void GetPublicHolidaysBetween_WithUnsupportedCountryCode_ReturnsEmptyList()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        // Act
        var result = _service.GetPublicHolidaysBetween(startDate, endDate, "US");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void IsPublicHoliday_ForNewYearsDay_ReturnsTrue()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);

        // Act
        var result = _service.IsPublicHoliday(date, "CZ");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPublicHoliday_ForNonHoliday_ReturnsFalse()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);

        // Act
        var result = _service.IsPublicHoliday(date, "CZ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPublicHoliday_WithUnsupportedCountryCode_ReturnsFalse()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);

        // Act
        var result = _service.IsPublicHoliday(date, "US");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetNextPublicHoliday_FromJanuary2_ReturnsNextHoliday()
    {
        // Arrange
        var date = new DateTime(2024, 1, 2);

        // Act
        var result = _service.GetNextPublicHoliday(date, "CZ");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Date > date);
    }

    [Fact]
    public void GetNextPublicHoliday_FromDecember27_ReturnsNextYearHoliday()
    {
        // Arrange
        var date = new DateTime(2024, 12, 27);

        // Act
        var result = _service.GetNextPublicHoliday(date, "CZ");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2025, result.Date.Year);
        Assert.Equal(1, result.Date.Month);
        Assert.Equal(1, result.Date.Day);
        Assert.Equal("Nový rok", result.Name);
    }

    [Fact]
    public void GetNextPublicHoliday_WithUnsupportedCountryCode_ReturnsNull()
    {
        // Arrange
        var date = new DateTime(2024, 1, 2);

        // Act
        var result = _service.GetNextPublicHoliday(date, "US");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPublicHolidaysForYear_IncludesEasterMonday()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.GetPublicHolidaysForYear(year, "CZ");

        // Assert
        Assert.Contains(result, h => h.Name == "Velikonoční pondělí");
    }

    [Fact]
    public void GetPublicHolidaysForYear_IncludesGoodFriday()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.GetPublicHolidaysForYear(year, "CZ");

        // Assert
        Assert.Contains(result, h => h.Name == "Velký pátek");
    }

    [Fact]
    public void GetPublicHoliday_CzechCharactersPreserved_InLocalName()
    {
        // Arrange - Test various holidays with Czech-specific characters
        var testCases = new[]
        {
            (Month: 1, Day: 1, ExpectedChar: 'ý'),  // Nový rok
            (Month: 5, Day: 1, ExpectedChar: 'á'),  // Svátek práce
            (Month: 9, Day: 28, ExpectedChar: 'č'), // české
            (Month: 12, Day: 24, ExpectedChar: 'ě')  // Štědrý
        };

        foreach (var (month, day, expectedChar) in testCases)
        {
            // Arrange
            var date = new DateTime(2024, month, day);

            // Act
            var result = _service.GetPublicHoliday(date, "CZ");

            // Assert
            Assert.NotNull(result);
            Assert.Contains(expectedChar, result.Name);
        }
    }
}
