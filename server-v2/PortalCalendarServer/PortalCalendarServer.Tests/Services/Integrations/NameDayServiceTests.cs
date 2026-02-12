using Microsoft.Extensions.Logging;
using Moq;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Tests.Services.Integrations;

/// <summary>
/// Unit tests for NameDayService
/// </summary>
public class NameDayServiceTests
{
    private readonly Mock<ILogger<NameDayService>> _mockLogger;
    private readonly NameDayService _service;

    public NameDayServiceTests()
    {
        _mockLogger = new Mock<ILogger<NameDayService>>();
        _service = new NameDayService(_mockLogger.Object);
    }

    [Fact]
    public void GetNameDay_ForJanuary1_ReturnsNovyRok()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);

        // Act
        var result = _service.GetNameDay(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Nový rok", result.Name);
        Assert.Equal(date, result.Date);
        Assert.Equal("CZ", result.CountryCode);
    }

    [Fact]
    public void GetNameDay_ForValentinesDay_ReturnsValentyn()
    {
        // Arrange
        var date = new DateTime(2024, 2, 14);

        // Act
        var result = _service.GetNameDay(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Valentýn / Valentýna", result.Name);
        Assert.Equal(date, result.Date);
        Assert.Equal("CZ", result.CountryCode);
    }

    [Fact]
    public void GetNameDay_ForChristmasEve_ReturnsAdamAEva()
    {
        // Arrange
        var date = new DateTime(2024, 12, 24);

        // Act
        var result = _service.GetNameDay(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Adam a Eva, Štìdrý den", result.Name);
        Assert.Equal(date, result.Date);
        Assert.Equal("CZ", result.CountryCode);
    }

    [Fact]
    public void GetNameDay_ForChristmasDay_ReturnsBoziHod()
    {
        // Arrange
        var date = new DateTime(2024, 12, 25);

        // Act
        var result = _service.GetNameDay(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Boží hod vánoèní, 1.svátek vánoèní", result.Name);
    }

    [Fact]
    public void GetNameDay_ForNewYearsEve_ReturnsSilvestr()
    {
        // Arrange
        var date = new DateTime(2024, 12, 31);

        // Act
        var result = _service.GetNameDay(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Silvestr", result.Name);
    }

    [Fact]
    public void GetNameDay_WithDifferentYears_ReturnsSameNameForSameDayMonth()
    {
        // Arrange
        var date2024 = new DateTime(2024, 3, 19);
        var date2025 = new DateTime(2025, 3, 19);

        // Act
        var result2024 = _service.GetNameDay(date2024);
        var result2025 = _service.GetNameDay(date2025);

        // Assert
        Assert.NotNull(result2024);
        Assert.NotNull(result2025);
        Assert.Equal("Josef", result2024.Name);
        Assert.Equal(result2024.Name, result2025.Name);
    }

    [Fact]
    public void GetNameDay_ForLeapYearDate_ReturnsCorrectNameDay()
    {
        // Arrange
        var date = new DateTime(2024, 2, 29); // Leap year

        // Act
        var result = _service.GetNameDay(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Horymír", result.Name);
    }

    [Fact]
    public void GetNameDay_WithUnsupportedCountryCode_ReturnsNull()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);

        // Act
        var result = _service.GetNameDay(date, "US");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(1, 1, "Nový rok")]
    [InlineData(1, 15, "Alice")]
    [InlineData(2, 14, "Valentýn / Valentýna")]
    [InlineData(3, 8, "Gabriela")]
    [InlineData(4, 27, "Jaroslav")]
    [InlineData(5, 1, "Svátek práce")]
    [InlineData(6, 24, "Jan")]
    [InlineData(7, 5, "Den slovanských vìrozvìstù Cyrila a Metodìje")]
    [InlineData(8, 15, "Hana")]
    [InlineData(9, 28, "Václav")]
    [InlineData(10, 28, "Alfréd")]
    [InlineData(11, 17, "Mahulena / Gertruda")]
    [InlineData(12, 6, "Mikuláš")]
    public void GetNameDay_ForVariousDates_ReturnsCorrectNames(int month, int day, string expectedName)
    {
        // Arrange
        var date = new DateTime(2024, month, day);

        // Act
        var result = _service.GetNameDay(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedName, result.Name);
    }

    [Fact]
    public void GetNameDaysForMonth_ForJanuary_Returns31Entries()
    {
        // Arrange
        var year = 2024;
        var month = 1;

        // Act
        var result = _service.GetNameDaysForMonth(year, month);

        // Assert
        Assert.Equal(31, result.Count);
        Assert.All(result, nd => Assert.Equal(month, nd.Date.Month));
        Assert.All(result, nd => Assert.Equal(year, nd.Date.Year));
    }

    [Fact]
    public void GetNameDaysForMonth_ForFebruary2024LeapYear_Returns29Entries()
    {
        // Arrange
        var year = 2024;
        var month = 2;

        // Act
        var result = _service.GetNameDaysForMonth(year, month);

        // Assert
        Assert.Equal(29, result.Count);
    }

    [Fact]
    public void GetNameDaysForMonth_ForFebruary2023NonLeapYear_Returns28Entries()
    {
        // Arrange
        var year = 2023;
        var month = 2;

        // Act
        var result = _service.GetNameDaysForMonth(year, month);

        // Assert
        Assert.Equal(28, result.Count);
    }

    [Fact]
    public void GetNameDaysForMonth_ForDecember_Returns31Entries()
    {
        // Arrange
        var year = 2024;
        var month = 12;

        // Act
        var result = _service.GetNameDaysForMonth(year, month);

        // Assert
        Assert.Equal(31, result.Count);
        Assert.Contains(result, nd => nd.Name == "Mikuláš");
        Assert.Contains(result, nd => nd.Name == "Silvestr");
    }

    [Fact]
    public void GetNameDaysForMonth_WithUnsupportedCountryCode_ReturnsEmptyList()
    {
        // Arrange
        var year = 2024;
        var month = 1;

        // Act
        var result = _service.GetNameDaysForMonth(year, month, "US");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetNameDaysForMonth_EntriesAreInChronologicalOrder()
    {
        // Arrange
        var year = 2024;
        var month = 3;

        // Act
        var result = _service.GetNameDaysForMonth(year, month);

        // Assert
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].Date < result[i + 1].Date);
        }
    }

    [Fact]
    public void FindNameDays_ForJosef_ReturnsMatch()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.FindNameDays("Josef", year);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, nd => nd.Date.Month == 3 && nd.Date.Day == 19);
        Assert.All(result, nd => Assert.Equal("CZ", nd.CountryCode));
    }

    [Fact]
    public void FindNameDays_ForPetr_ReturnsMultipleMatches()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.FindNameDays("Petr", year);

        // Assert
        Assert.NotEmpty(result);
        // Petr appears on February 22 and June 29 (Petr a Pavel)
        Assert.True(result.Count >= 2);
        Assert.Contains(result, nd => nd.Date.Month == 2 && nd.Date.Day == 22);
        Assert.Contains(result, nd => nd.Date.Month == 6 && nd.Date.Day == 29);
    }

    [Fact]
    public void FindNameDays_CaseInsensitive_FindsMatches()
    {
        // Arrange
        var year = 2024;

        // Act
        var resultLower = _service.FindNameDays("josef", year);
        var resultUpper = _service.FindNameDays("JOSEF", year);
        var resultMixed = _service.FindNameDays("JoSeF", year);

        // Assert
        Assert.NotEmpty(resultLower);
        Assert.NotEmpty(resultUpper);
        Assert.NotEmpty(resultMixed);
        Assert.Equal(resultLower.Count, resultUpper.Count);
        Assert.Equal(resultLower.Count, resultMixed.Count);
    }

    [Fact]
    public void FindNameDays_ForPartialName_FindsMatches()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.FindNameDays("Mar", year);

        // Assert
        Assert.NotEmpty(result);
        // Should find Marika, Marie, Marián, Marcela, Marek, Marina, etc.
        Assert.Contains(result, nd => nd.Name.Contains("Marika"));
        Assert.Contains(result, nd => nd.Name.Contains("Marie"));
    }

    [Fact]
    public void FindNameDays_WithEmptyString_ReturnsEmptyList()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.FindNameDays("", year);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindNameDays_WithNull_ReturnsEmptyList()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.FindNameDays(null!, year);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindNameDays_WithWhitespace_ReturnsEmptyList()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.FindNameDays("   ", year);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindNameDays_ForNonExistentName_ReturnsEmptyList()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.FindNameDays("XyzNotAName", year);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindNameDays_WithUnsupportedCountryCode_ReturnsEmptyList()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.FindNameDays("Josef", year, "US");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindNameDays_ResultsAreOrderedByDate()
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.FindNameDays("an", year); // Should find multiple names containing "an"

        // Assert
        Assert.NotEmpty(result);
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].Date <= result[i + 1].Date);
        }
    }

    [Theory]
    [InlineData("Václav", 9, 28)]
    [InlineData("Martin", 11, 11)]
    [InlineData("Kateøina", 11, 25)]
    [InlineData("Lucie", 12, 13)]
    public void FindNameDays_ForPopularNames_ReturnsCorrectDate(string name, int expectedMonth, int expectedDay)
    {
        // Arrange
        var year = 2024;

        // Act
        var result = _service.FindNameDays(name, year);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, nd => nd.Date.Month == expectedMonth && nd.Date.Day == expectedDay);
    }

    [Fact]
    public void GetNameDay_ForAllDaysInYear_ReturnsValidResults()
    {
        // Arrange
        var year = 2024;
        var date = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31);
        var count = 0;

        // Act & Assert
        while (date <= endDate)
        {
            var result = _service.GetNameDay(date);
            Assert.NotNull(result);
            Assert.Equal(date, result.Date);
            Assert.Equal("CZ", result.CountryCode);
            Assert.False(string.IsNullOrWhiteSpace(result.Name));
            
            count++;
            date = date.AddDays(1);
        }

        // Verify we tested all 366 days (2024 is a leap year)
        Assert.Equal(366, count);
    }

    [Fact]
    public void GetNameDay_CzechCharactersPreserved_InResults()
    {
        // Arrange - Test various dates with Czech-specific characters
        var testCases = new[]
        {
            (Month: 1, Day: 23, ExpectedChar: 'ì'), // Zdenìk
            (Month: 2, Day: 3, ExpectedChar: 'ž'),  // Blažej
            (Month: 3, Day: 13, ExpectedChar: 'ù'), // Rùžena
            (Month: 5, Day: 15, ExpectedChar: 'Ž'), // Žofie / Sofie
            (Month: 9, Day: 28, ExpectedChar: 'á'), // Václav
            (Month: 11, Day: 25, ExpectedChar: 'ø') // Kateøina
        };

        foreach (var (month, day, expectedChar) in testCases)
        {
            // Arrange
            var date = new DateTime(2024, month, day);

            // Act
            var result = _service.GetNameDay(date);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(expectedChar, result.Name);
        }
    }
}
