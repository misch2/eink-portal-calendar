using Microsoft.Extensions.Logging;
using Moq;
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
    public void GetPublicHoliday_ForNewYearsDay_ReturnsHoliday()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);

        // Act
        var result = _service.GetPublicHoliday(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Year's Day", result.Name);
        Assert.Equal("Nový rok", result.LocalName);
        Assert.Equal(date.Date, result.Date.Date);
        Assert.Equal("CZ", result.CountryCode);
    }

    [Fact]
    public void GetPublicHoliday_ForLabourDay_ReturnsHoliday()
    {
        // Arrange
        var date = new DateTime(2024, 5, 1);

        // Act
        var result = _service.GetPublicHoliday(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Labour Day", result.Name);
        Assert.Equal("Svátek práce", result.LocalName);
        Assert.Equal(date.Date, result.Date.Date);
    }

    [Fact]
    public void GetPublicHoliday_ForChristmasDay_ReturnsHoliday()
    {
        // Arrange
        var date = new DateTime(2024, 12, 25);

        // Act
        var result = _service.GetPublicHoliday(date);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Christmas", result.Name);
        Assert.Equal(date.Date, result.Date.Date);
    }

    [Fact]
    public void GetPublicHoliday_ForNonHoliday_ReturnsNull()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15); // Regular Monday

        // Act
        var result = _service.GetPublicHoliday(date);

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
        var result2024 = _service.GetPublicHoliday(date2024);
        var result2025 = _service.GetPublicHoliday(date2025);

        // Assert
        Assert.NotNull(result2024);
        Assert.NotNull(result2025);
        Assert.Equal(result2024.Name, result2025.Name);
        Assert.Equal(result2024.LocalName, result2025.LocalName);
    }

    [Theory]
    [InlineData(1, 1, "New Year's Day", "Nový rok")]
    [InlineData(5, 1, "Labour Day", "Svátek práce")]
    [InlineData(5, 8, "Liberation Day", "Den vítìzství")]
    [InlineData(7, 5, "Saints Cyril and Methodius Day", "Den slovanských vìrozvìstù Cyrila a Metodìje")]
    [InlineData(7, 6, "Jan Hus Day", "Den upálení mistra Jana Husa")]
    [InlineData(9, 28, "St. Wenceslas Day", "Den èeské státnosti")]
    [InlineData(10, 28, "Independent Czechoslovak State Day", "Den vzniku samostatného èeskoslovenského státu")]
    [InlineData(11, 17, "Struggle for Freedom and Democracy Day", "Den boje za svobodu a demokracii")]
    [InlineData(12, 24, "Christmas Eve", "Štìdrý den")]
    [InlineData(12, 25, "Christmas Day", "1. svátek vánoèní")]
    [InlineData(12, 26, "St. Stephen's Day", "2. svátek vánoèní")]
    public void GetPublicHoliday_ForVariousHolidays_ReturnsCorrectInfo(
        int month, int day, string expectedName, string expectedLocalName)
    {
        // Arrange
        var date = new DateTime(2024, month, day);

        // Act
        var result = _service.GetPublicHoliday(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedName, result.Name);
        Assert.Equal(expectedLocalName, result.LocalName);
    }

    [Fact]
    public void GetPublicHolidaysForYear_For2024_ReturnsAllHolidays()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.GetPublicHolidaysForYear(year);

        // Assert
        Assert.NotEmpty(result);
        // Czech Republic has 13 fixed public holidays + movable Easter holidays
        Assert.True(result.Count >= 13);
        Assert.All(result, h => Assert.Equal(year, h.Date.Year));
        Assert.All(result, h => Assert.Equal("CZ", h.CountryCode));
    }

    [Fact]
    public void GetPublicHolidaysForYear_ResultsAreOrdered()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.GetPublicHolidaysForYear(year);

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
        var result = _service.GetPublicHolidaysBetween(startDate, endDate);

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
        var result = _service.GetPublicHolidaysBetween(startDate, endDate);

        // Assert
        var yearHolidays = _service.GetPublicHolidaysForYear(2024);
        Assert.Equal(yearHolidays.Count, result.Count);
    }

    [Fact]
    public void GetPublicHolidaysBetween_SpanningMultipleYears_ReturnsCorrectHolidays()
    {
        // Arrange
        var startDate = new DateTime(2023, 12, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var result = _service.GetPublicHolidaysBetween(startDate, endDate);

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
        var result = _service.GetPublicHolidaysBetween(startDate, endDate);

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
        var result = _service.IsPublicHoliday(date);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPublicHoliday_ForNonHoliday_ReturnsFalse()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);

        // Act
        var result = _service.IsPublicHoliday(date);

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
        var result = _service.GetNextPublicHoliday(date);

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
        var result = _service.GetNextPublicHoliday(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2025, result.Date.Year);
        Assert.Equal(1, result.Date.Month);
        Assert.Equal(1, result.Date.Day);
        Assert.Equal("New Year's Day", result.Name);
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
        var result = _service.GetPublicHolidaysForYear(year);

        // Assert
        Assert.Contains(result, h => h.Name == "Easter Monday");
    }

    [Fact]
    public void GetPublicHolidaysForYear_IncludesGoodFriday()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.GetPublicHolidaysForYear(year);

        // Assert
        Assert.Contains(result, h => h.Name == "Good Friday");
    }

    [Fact]
    public void GetPublicHoliday_CzechCharactersPreserved_InLocalName()
    {
        // Arrange - Test various holidays with Czech-specific characters
        var testCases = new[]
        {
            (Month: 1, Day: 1, ExpectedChar: 'ý'),  // Nový rok
            (Month: 5, Day: 1, ExpectedChar: 'á'),  // Svátek práce
            (Month: 9, Day: 28, ExpectedChar: 'è'), // èeské
            (Month: 12, Day: 24, ExpectedChar: 'ì')  // Štìdrý
        };

        foreach (var (month, day, expectedChar) in testCases)
        {
            // Arrange
            var date = new DateTime(2024, month, day);

            // Act
            var result = _service.GetPublicHoliday(date);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(expectedChar, result.LocalName);
        }
    }
}
