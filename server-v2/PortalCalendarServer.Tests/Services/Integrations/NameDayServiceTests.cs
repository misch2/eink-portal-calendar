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
    [InlineData(9, 29, "Michal")]
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
}
