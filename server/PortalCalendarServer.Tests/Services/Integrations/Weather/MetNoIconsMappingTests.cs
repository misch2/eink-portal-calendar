using PortalCalendarServer.Services.Integrations.Weather;

namespace PortalCalendarServer.Tests.Services.Integrations.Weather;

/// <summary>
/// Unit tests for Met.no icon mapping
/// </summary>
public class MetNoIconsMappingTests
{
    private readonly MetNoIconsMapping _mapping = new();

    [Theory]
    [InlineData("clearsky_day", 800)]
    [InlineData("fair_night", 801)]
    [InlineData("partlycloudy_polartwilight", 801)]
    [InlineData("cloudy", 804)]
    [InlineData("lightrainshowers", 500)]
    [InlineData("rainshowers", 501)]
    [InlineData("heavyrainshowers", 502)]
    [InlineData("fog", 741)]
    public void MapSymbol_WithVariousCodes_ReturnsCorrectIds(string code, int expectedId)
    {
        // Act
        var result = _mapping.MapSymbol(code);

        // Assert
        Assert.Equal(expectedId, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown_weather_code")]
    public void MapSymbol_WithInvalidInput_ReturnsNull(string? code)
    {
        // Act
        var result = _mapping.MapSymbol(code);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("clearsky", "cz", "Jasno")]
    [InlineData("clearsky", "en", "Clear sky")]
    [InlineData("clearsky_day", "cz", "Jasno")] // Also tests suffix stripping
    [InlineData("rain", "cz", "DÈöù")]
    [InlineData("snow", "cz", "SnÌh")]
    [InlineData("fog", "cz", "Mlha")]
    [InlineData("cloudy", "cz", "Zataûeno")]
    [InlineData("rainandthunder", "cz", "DÈöù a bou¯ky")]
    public void MapDescription_WithVariousInputs_ReturnsCorrectTranslations(
        string code, string language, string expectedDescription)
    {
        // Act
        var result = _mapping.MapDescription(code, language);

        // Assert
        Assert.Equal(expectedDescription, result);
    }

    [Fact]
    public void MapDescription_WithoutLanguageParameter_DefaultsToCzech()
    {
        // Act
        var result = _mapping.MapDescription("clearsky");

        // Assert
        Assert.Equal("Jasno", result);
    }

    [Fact]
    public void GetSymbolDetails_WithValidCode_ReturnsCompleteDetails()
    {
        // Act
        var result = _mapping.GetSymbolDetails("clearsky_day");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("clearsky", result.Code);
        Assert.Equal(800, result.OpenWeatherId);
        Assert.Equal("Clear sky", result.DescriptionEn);
        Assert.Equal("Jasno", result.DescriptionCz);
    }

    [Fact]
    public void GetSymbolDetails_WithNullCode_ReturnsNull()
    {
        // Act
        var result = _mapping.GetSymbolDetails(null);

        // Assert
        Assert.Null(result);
    }
}
