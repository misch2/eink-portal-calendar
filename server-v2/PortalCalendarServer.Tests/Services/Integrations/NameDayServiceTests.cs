using Microsoft.Extensions.Logging;
using Moq;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Tests.Services.Integrations;

/// <summary>
/// Unit tests for NameDayService
/// </summary>
public class NameDayServiceTests
{
    private readonly NameDayService _service = new(new Mock<ILogger<NameDayService>>().Object);

    [Theory]
    [InlineData(1, 1, "Nový rok")]
    [InlineData(1, 15, "Alice")]
    [InlineData(2, 14, "Valentýn")]
    [InlineData(2, 29, "Horymír")] // Leap year
    [InlineData(3, 8, "Gabriela")]
    [InlineData(3, 19, "Josef")]
    [InlineData(4, 27, "Jaroslav")]
    [InlineData(5, 1, "Svátek práce")]
    [InlineData(6, 24, "Jan")]
    [InlineData(7, 5, "Den slovanských vìrozvìstù Cyrila a Metodìje")]
    [InlineData(8, 15, "Hana")]
    [InlineData(9, 28, "Václav")]
    [InlineData(9, 29, "Michal")]
    [InlineData(10, 28, "Alfréd")]
    [InlineData(11, 17, "Mahulena+Gertruda")]
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

    [Theory]
    [InlineData(2024, 1, 31, "Nový rok", "Marika")]
    [InlineData(2024, 2, 29, null, "Horymír")]      // Leap year
    [InlineData(2023, 2, 28, null, "Lumír")]        // Non-leap year
    [InlineData(2024, 12, 31, "Mikuláš", "Silvestr")]
    public void GetNameDaysForMonth_ForVariousMonths_ReturnsCorrectEntries(
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
        
        // Verify chronological ordering
        for (int i = 0; i < result.Count - 1; i++)
            Assert.True(result[i].Date < result[i + 1].Date);
    }

    [Fact]
    public void GetNameDay_WithUnsupportedCountryCode_ReturnsNull()
    {
        Assert.Null(_service.GetNameDay(new DateTime(2024, 1, 1), "US"));
    }

    [Fact]
    public void GetNameDaysForMonth_WithUnsupportedCountryCode_ReturnsEmptyList()
    {
        Assert.Empty(_service.GetNameDaysForMonth(2024, 1, "US"));
    }

    [Fact]
    public void GetNameDay_WithDifferentYears_ReturnsSameNameForSameDayMonth()
    {
        var result2024 = _service.GetNameDay(new DateTime(2024, 3, 19));
        var result2025 = _service.GetNameDay(new DateTime(2025, 3, 19));

        Assert.NotNull(result2024);
        Assert.NotNull(result2025);
        Assert.Equal("Josef", result2024.Name);
        Assert.Equal(result2024.Name, result2025.Name);
    }

    [Fact]
    public void GetNameDay_ForAllDaysInYear_ReturnsValidResults()
    {
        var date = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var count = 0;

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

        Assert.Equal(366, count); // 2024 is a leap year
    }
}
