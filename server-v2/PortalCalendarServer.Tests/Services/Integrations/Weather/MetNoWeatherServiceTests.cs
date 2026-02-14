using Microsoft.Extensions.Logging;
using Moq.Protected;
using PortalCalendarServer.Services.Integrations.Weather;
using PortalCalendarServer.Tests.TestBase;
using System.Net;

namespace PortalCalendarServer.Tests.Services.Integrations.Weather;

/// <summary>
/// Unit tests for Met.no weather service
/// </summary>
public class MetNoWeatherServiceTests : IntegrationServiceTestBase
{
    private const double TestLat = 50.0755;
    private const double TestLon = 14.4378;
    private const double TestAlt = 200;

    private MetNoWeatherService CreateService()
    {
        var logger = new Mock<ILogger<MetNoWeatherService>>().Object;
        return new MetNoWeatherService(
            logger,
            MockHttpClientFactory.Object,
            MemoryCache,
            Context,
            TestLat,
            TestLon,
            TestAlt,
            TestDisplay);
    }

    [Fact]
    public async Task GetCurrentAsync_WithValidResponse_ReturnsWeatherData()
    {
        // Arrange
        var service = CreateService();
        var sampleJson = @"{
            ""properties"": {
                ""meta"": { ""updated_at"": ""2024-02-14T12:00:00Z"" },
                ""timeseries"": [{
                    ""time"": ""2024-02-14T12:00:00Z"",
                    ""data"": {
                        ""instant"": {
                            ""details"": {
                                ""air_temperature"": 5.2,
                                ""air_pressure_at_sea_level"": 1013.5,
                                ""relative_humidity"": 75.0,
                                ""cloud_area_fraction"": 50.0,
                                ""fog_area_fraction"": 0.0,
                                ""wind_speed"": 3.5,
                                ""wind_from_direction"": 180.0
                            }
                        },
                        ""next_1_hours"": {
                            ""summary"": { ""symbol_code"": ""partlycloudy_day"" },
                            ""details"": { ""precipitation_amount"": 0.0 }
                        }
                    }
                }]
            }
        }";

        // Setup HTTP response for any URL containing the API endpoint
        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("api.met.no/weatherapi/locationforecast")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sampleJson)
            });

        // Act
        var result = await service.GetCurrentAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("met.no", result.Provider);
        Assert.Equal(5.2, result.Temperature);
        Assert.Equal(1013.5, result.PressureAtSeaLevel);
        Assert.Equal(75.0, result.Humidity);
        Assert.Equal(50.0, result.CloudPercent);
        Assert.Equal(3.5, result.WindSpeed);
        Assert.Equal(180.0, result.WindFrom);
        Assert.Equal("partlycloudy_day", result.ProviderSymbolCode);
        Assert.Equal(801, result.WiSymbolCode);
    }

    [Fact]
    public async Task GetForecastAsync_WithValidResponse_ReturnsMultipleDataPoints()
    {
        // Arrange
        var service = CreateService();
        var sampleJson = @"{
            ""properties"": {
                ""meta"": { ""updated_at"": ""2024-02-14T12:00:00Z"" },
                ""timeseries"": [
                    {
                        ""time"": ""2024-02-14T12:00:00Z"",
                        ""data"": {
                            ""instant"": { ""details"": { ""air_temperature"": 5.2, ""air_pressure_at_sea_level"": 1013.5, ""relative_humidity"": 75.0, ""cloud_area_fraction"": 50.0, ""fog_area_fraction"": 0.0, ""wind_speed"": 3.5, ""wind_from_direction"": 180.0 } },
                            ""next_1_hours"": { ""summary"": { ""symbol_code"": ""partlycloudy_day"" }, ""details"": { ""precipitation_amount"": 0.0 } }
                        }
                    },
                    {
                        ""time"": ""2024-02-14T13:00:00Z"",
                        ""data"": {
                            ""instant"": { ""details"": { ""air_temperature"": 6.0, ""air_pressure_at_sea_level"": 1013.0, ""relative_humidity"": 70.0, ""cloud_area_fraction"": 40.0, ""fog_area_fraction"": 0.0, ""wind_speed"": 4.0, ""wind_from_direction"": 190.0 } },
                            ""next_1_hours"": { ""summary"": { ""symbol_code"": ""clearsky_day"" }, ""details"": { ""precipitation_amount"": 0.0 } }
                        }
                    }
                ]
            }
        }";

        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("api.met.no/weatherapi/locationforecast")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sampleJson)
            });

        // Act
        var result = await service.GetForecastAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(5.2, result[0].Temperature);
        Assert.Equal(6.0, result[1].Temperature);
    }

    [Fact]
    public async Task GetForecastAsync_SkipsEntriesWithoutNext1Hours()
    {
        // Arrange
        var service = CreateService();
        var sampleJson = @"{
            ""properties"": {
                ""meta"": { ""updated_at"": ""2024-02-14T12:00:00Z"" },
                ""timeseries"": [
                    {
                        ""time"": ""2024-02-14T12:00:00Z"",
                        ""data"": {
                            ""instant"": { ""details"": { ""air_temperature"": 5.2, ""air_pressure_at_sea_level"": 1013.5, ""relative_humidity"": 75.0, ""cloud_area_fraction"": 50.0, ""fog_area_fraction"": 0.0, ""wind_speed"": 3.5, ""wind_from_direction"": 180.0 } }
                        }
                    }
                ]
            }
        }";

        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("api.met.no/weatherapi/locationforecast")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sampleJson)
            });

        // Act
        var result = await service.GetForecastAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Aggregate_WithMultipleForecasts_CalculatesCorrectAverages()
    {
        // Arrange
        var service = CreateService();
        var forecasts = new List<PortalCalendarServer.Models.Weather.WeatherData>
        {
            new() {
                Provider = "met.no",
                Temperature = 5.0,
                PressureAtSeaLevel = 1010.0,
                Humidity = 70.0,
                CloudPercent = 40.0,
                FogPercent = 0.0,
                WindSpeed = 3.0,
                WindFrom = 180.0,
                Precipitation = 0.0,
                TimeStart = new DateTime(2024, 2, 14, 12, 0, 0, DateTimeKind.Utc),
                TimeEnd = new DateTime(2024, 2, 14, 13, 0, 0, DateTimeKind.Utc),
                TimeIsDay = true,
                UpdatedAt = DateTime.UtcNow
            },
            new() {
                Provider = "met.no",
                Temperature = 7.0,
                PressureAtSeaLevel = 1012.0,
                Humidity = 75.0,
                CloudPercent = 60.0,
                FogPercent = 10.0,
                WindSpeed = 5.0,
                WindFrom = 190.0,
                Precipitation = 0.5,
                TimeStart = new DateTime(2024, 2, 14, 13, 0, 0, DateTimeKind.Utc),
                TimeEnd = new DateTime(2024, 2, 14, 14, 0, 0, DateTimeKind.Utc),
                TimeIsDay = true,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = service.Aggregate(forecasts, new DateTime(2024, 2, 14, 12, 0, 0, DateTimeKind.Utc), 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.AggregatedCount);
        Assert.Equal(5.0, result.TemperatureMin);
        Assert.Equal(7.0, result.TemperatureMax);
        Assert.Equal(6.0, result.TemperatureAvg);
        Assert.Equal(0.5, result.PrecipitationSum);
    }

    [Fact]
    public void Aggregate_WithNoMatchingForecasts_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        var forecasts = new List<PortalCalendarServer.Models.Weather.WeatherData>
        {
            new() {
                Provider = "met.no",
                Temperature = 5.0,
                TimeStart = new DateTime(2024, 2, 14, 10, 0, 0, DateTimeKind.Utc),
                TimeEnd = new DateTime(2024, 2, 14, 11, 0, 0, DateTimeKind.Utc),
                TimeIsDay = true
            }
        };

        // Act
        var result = service.Aggregate(forecasts, new DateTime(2024, 2, 14, 12, 0, 0, DateTimeKind.Utc), 2);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentAsync_UsesCaching()
    {
        // Arrange
        var service = CreateService();
        var sampleJson = @"{
            ""properties"": {
                ""meta"": { ""updated_at"": ""2024-02-14T12:00:00Z"" },
                ""timeseries"": [{
                    ""time"": ""2024-02-14T12:00:00Z"",
                    ""data"": {
                        ""instant"": { ""details"": { ""air_temperature"": 5.2, ""air_pressure_at_sea_level"": 1013.5, ""relative_humidity"": 75.0, ""cloud_area_fraction"": 50.0, ""fog_area_fraction"": 0.0, ""wind_speed"": 3.5, ""wind_from_direction"": 180.0 } },
                        ""next_1_hours"": { ""summary"": { ""symbol_code"": ""partlycloudy_day"" }, ""details"": { ""precipitation_amount"": 0.0 } }
                    }
                }]
            }
        }";

        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("api.met.no/weatherapi/locationforecast")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sampleJson)
            });

        // Act - First call
        var result1 = await service.GetCurrentAsync();

        // Act - Second call (should use cache)
        var result2 = await service.GetCurrentAsync();

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Temperature, result2.Temperature);

        // Verify HTTP was called only once
        MockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("api.met.no/weatherapi/locationforecast")),
                ItExpr.IsAny<CancellationToken>());
    }
}
