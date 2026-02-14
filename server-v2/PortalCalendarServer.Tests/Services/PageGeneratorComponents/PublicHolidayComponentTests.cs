using Microsoft.Extensions.Logging;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Integrations;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Tests.Services.PageGeneratorComponents;

/// <summary>
/// Unit tests for PublicHolidayComponent
/// </summary>
public class PublicHolidayComponentTests
{
    private readonly Mock<ILogger<PageGeneratorService>> _mockLogger;
    private readonly Mock<IPublicHolidayService> _mockPublicHolidayService;

    public PublicHolidayComponentTests()
    {
        _mockLogger = new Mock<ILogger<PageGeneratorService>>();
        _mockPublicHolidayService = new Mock<IPublicHolidayService>();
    }

    private PublicHolidayComponent CreateComponent()
    {
        return new PublicHolidayComponent(
            _mockLogger.Object,
            _mockPublicHolidayService.Object);
    }

    [Fact]
    public void GetPublicHolidayInfo_CallsPublicHolidayService()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);
        var expectedInfo = new PublicHolidayInfo
        {
            Name = "New Year's Day",
            LocalName = "Nový rok",
            Date = date,
            CountryCode = "CZ"
        };

        _mockPublicHolidayService
            .Setup(s => s.GetPublicHoliday(date, It.IsAny<string>()))
            .Returns(expectedInfo);

        var component = CreateComponent();

        // Act
        var result = component.GetPublicHolidayInfo(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Year's Day", result.Name);
        Assert.Equal(date, result.Date);
        _mockPublicHolidayService.Verify(s => s.GetPublicHoliday(date, It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void GetPublicHolidayInfo_WhenNoHoliday_ReturnsNull()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);

        _mockPublicHolidayService
            .Setup(s => s.GetPublicHoliday(It.IsAny<DateTime>(), It.IsAny<string>()))
            .Returns((PublicHolidayInfo?)null);

        var component = CreateComponent();

        // Act
        var result = component.GetPublicHolidayInfo(date);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPublicHolidayInfo_UsesDifferentDates()
    {
        // Arrange
        var date1 = new DateTime(2024, 1, 1);
        var date2 = new DateTime(2024, 12, 25);

        _mockPublicHolidayService
            .Setup(s => s.GetPublicHoliday(date1, It.IsAny<string>()))
            .Returns(new PublicHolidayInfo { Name = "New Year's Day", Date = date1, CountryCode = "CZ" });

        _mockPublicHolidayService
            .Setup(s => s.GetPublicHoliday(date2, It.IsAny<string>()))
            .Returns(new PublicHolidayInfo { Name = "Christmas Day", Date = date2, CountryCode = "CZ" });

        var component = CreateComponent();

        // Act
        var result1 = component.GetPublicHolidayInfo(date1);
        var result2 = component.GetPublicHolidayInfo(date2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("New Year's Day", result1.Name);
        Assert.Equal("Christmas Day", result2.Name);
        Assert.Equal(date1, result1.Date);
        Assert.Equal(date2, result2.Date);
    }

    [Fact]
    public void GetYearHolidays_CallsPublicHolidayServiceWithCorrectParameters()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);
        var expectedList = new List<PublicHolidayInfo>
        {
            new() { Name = "New Year's Day", Date = new DateTime(2024, 1, 1), CountryCode = "CZ" },
            new() { Name = "Christmas Day", Date = new DateTime(2024, 12, 25), CountryCode = "CZ" }
        };

        _mockPublicHolidayService
            .Setup(s => s.GetPublicHolidaysForYear(2024, It.IsAny<string>()))
            .Returns(expectedList);

        var component = CreateComponent();

        // Act
        var result = component.GetYearHolidays(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _mockPublicHolidayService.Verify(s => s.GetPublicHolidaysForYear(2024, It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void GetYearHolidays_UsesDifferentYears()
    {
        // Arrange
        var date2024 = new DateTime(2024, 6, 1);
        var date2025 = new DateTime(2025, 6, 1);

        _mockPublicHolidayService
            .Setup(s => s.GetPublicHolidaysForYear(2024, It.IsAny<string>()))
            .Returns(new List<PublicHolidayInfo>());

        _mockPublicHolidayService
            .Setup(s => s.GetPublicHolidaysForYear(2025, It.IsAny<string>()))
            .Returns(new List<PublicHolidayInfo>());

        var component = CreateComponent();

        // Act
        component.GetYearHolidays(date2024);
        component.GetYearHolidays(date2025);

        // Assert
        _mockPublicHolidayService.Verify(s => s.GetPublicHolidaysForYear(2024, It.IsAny<string>()), Times.Once());
        _mockPublicHolidayService.Verify(s => s.GetPublicHolidaysForYear(2025, It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void IsPublicHoliday_ForHoliday_ReturnsTrue()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);

        _mockPublicHolidayService
            .Setup(s => s.IsPublicHoliday(date, It.IsAny<string>()))
            .Returns(true);

        var component = CreateComponent();

        // Act
        var result = component.IsPublicHoliday(date);

        // Assert
        Assert.True(result);
        _mockPublicHolidayService.Verify(s => s.IsPublicHoliday(date, It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void IsPublicHoliday_ForNonHoliday_ReturnsFalse()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);

        _mockPublicHolidayService
            .Setup(s => s.IsPublicHoliday(date, It.IsAny<string>()))
            .Returns(false);

        var component = CreateComponent();

        // Act
        var result = component.IsPublicHoliday(date);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetNextHoliday_CallsPublicHolidayService()
    {
        // Arrange
        var date = new DateTime(2024, 1, 2);
        var expectedHoliday = new PublicHolidayInfo
        {
            Name = "Easter Monday",
            Date = new DateTime(2024, 4, 1),
            CountryCode = "CZ"
        };

        _mockPublicHolidayService
            .Setup(s => s.GetNextPublicHoliday(date, It.IsAny<string>()))
            .Returns(expectedHoliday);

        var component = CreateComponent();

        // Act
        var result = component.GetNextHoliday(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Easter Monday", result.Name);
        Assert.True(result.Date > date);
        _mockPublicHolidayService.Verify(s => s.GetNextPublicHoliday(date, It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void GetNextHoliday_WhenNoMoreHolidays_ReturnsNull()
    {
        // Arrange
        var date = new DateTime(2024, 12, 27);

        _mockPublicHolidayService
            .Setup(s => s.GetNextPublicHoliday(date, It.IsAny<string>()))
            .Returns((PublicHolidayInfo?)null);

        var component = CreateComponent();

        // Act
        var result = component.GetNextHoliday(date);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPublicHolidayInfo_WithProvidedDate_CallsServiceWithThatDate()
    {
        // Arrange
        var date = new DateTime(2024, 5, 1);
        _mockPublicHolidayService
            .Setup(s => s.GetPublicHoliday(date, It.IsAny<string>()))
            .Returns(new PublicHolidayInfo { Name = "Test Holiday", Date = date, CountryCode = "CZ" });

        var component = CreateComponent();

        // Act
        component.GetPublicHolidayInfo(date);

        // Assert
        _mockPublicHolidayService.Verify(s => s.GetPublicHoliday(date, It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void GetPublicHolidayInfo_LogsDebugMessage()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);
        _mockPublicHolidayService
            .Setup(s => s.GetPublicHoliday(It.IsAny<DateTime>(), It.IsAny<string>()))
            .Returns(new PublicHolidayInfo { Name = "Test Holiday", Date = date, CountryCode = "CZ" });

        var component = CreateComponent();

        // Act
        component.GetPublicHolidayInfo(date);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting public holiday information")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
