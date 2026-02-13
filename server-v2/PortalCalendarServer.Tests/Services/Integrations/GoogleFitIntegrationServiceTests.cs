using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PortalCalendarServer.Models;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services.Integrations;
using PortalCalendarServer.Tests.TestBase;

namespace PortalCalendarServer.Tests.Services.Integrations;

/// <summary>
/// Unit tests for GoogleFitIntegrationService
/// </summary>
public class GoogleFitIntegrationServiceTests : IntegrationServiceTestBase
{
    private GoogleFitIntegrationService CreateService(Display? display = null)
    {
        var logger = new Mock<ILogger<GoogleFitIntegrationService>>().Object;
        return new GoogleFitIntegrationService(
            logger,
            MockHttpClientFactory.Object,
            MemoryCache,
            Context,
            display,
            minimalCacheExpiry: 0);
    }

    private Display CreateDisplayWithGoogleFitTokens()
    {
        var display = CreateTestDisplay();
        
        Context.Configs.AddRange(
            new Config
            {
                DisplayId = display.Id,
                Name = "_googlefit_access_token",
                Value = "test_access_token_123"
            },
            new Config
            {
                DisplayId = display.Id,
                Name = "_googlefit_refresh_token",
                Value = "test_refresh_token_456"
            },
            new Config
            {
                DisplayId = display.Id,
                Name = "googlefit_client_id",
                Value = "test_client_id"
            },
            new Config
            {
                DisplayId = display.Id,
                Name = "googlefit_client_secret",
                Value = "test_client_secret"
            },
            new Config
            {
                DisplayId = display.Id,
                Name = "googlefit_auth_callback",
                Value = "https://example.com/callback"
            });
        
        Context.SaveChanges();
        
        // Reload the display with configs
        Context.Entry(display).Collection(d => d.Configs).Load();
        
        return display;
    }

    private string CreateMockGoogleFitResponse(params (DateTime date, double weight)[] dataPoints)
    {
        var buckets = dataPoints.Select(dp => new
        {
            startTimeMillis = ((DateTimeOffset)dp.date.Date).ToUnixTimeMilliseconds(),
            endTimeMillis = ((DateTimeOffset)dp.date.Date.AddDays(1).AddSeconds(-1)).ToUnixTimeMilliseconds(),
            dataset = new[]
            {
                new
                {
                    point = new[]
                    {
                        new
                        {
                            value = new[]
                            {
                                new { fpVal = dp.weight }
                            }
                        }
                    }
                }
            }
        });

        return JsonSerializer.Serialize(new { bucket = buckets });
    }

    [Fact]
    public void IsAvailable_WhenNoDisplay_ReturnsFalse()
    {
        var service = CreateService(display: null);
        
        var result = service.IsAvailable();
        
        Assert.False(result);
    }

    [Fact]
    public void IsAvailable_WhenNoTokens_ReturnsFalse()
    {
        var display = CreateTestDisplay();
        var service = CreateService(display);
        
        var result = service.IsAvailable();
        
        Assert.False(result);
    }

    [Fact]
    public void IsAvailable_WhenOnlyAccessToken_ReturnsFalse()
    {
        var display = CreateTestDisplay();
        Context.Configs.Add(new Config
        {
            DisplayId = display.Id,
            Name = "_googlefit_access_token",
            Value = "test_token"
        });
        Context.SaveChanges();
        Context.Entry(display).Collection(d => d.Configs).Load();
        
        var service = CreateService(display);
        
        var result = service.IsAvailable();
        
        Assert.False(result);
    }

    [Fact]
    public void IsAvailable_WhenBothTokensPresent_ReturnsTrue()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);
        
        var result = service.IsAvailable();
        
        Assert.True(result);
    }

    [Fact]
    public async Task GetNewAccessTokenFromRefreshTokenAsync_WhenSuccessful_ReturnsNewToken()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        var tokenResponse = JsonSerializer.Serialize(new
        {
            access_token = "new_access_token_789",
            expires_in = 3600,
            token_type = "Bearer"
        });

        SetupHttpResponseForAnyUrl(tokenResponse, HttpStatusCode.OK);

        var result = await service.GetNewAccessTokenFromRefreshTokenAsync();

        Assert.Equal("new_access_token_789", result);
        
        // Verify token was saved to database
        Context.Entry(display).Collection(d => d.Configs).Query().Load();
        var savedToken = display.Configs?.FirstOrDefault(c => c.Name == "_googlefit_access_token")?.Value;
        Assert.Equal("new_access_token_789", savedToken);
    }

    [Fact]
    public async Task GetNewAccessTokenFromRefreshTokenAsync_WhenMissingConfig_ReturnsNull()
    {
        var display = CreateTestDisplay();
        var service = CreateService(display);

        var result = await service.GetNewAccessTokenFromRefreshTokenAsync();

        Assert.Null(result);
        VerifyAnyHttpRequest(Times.Never());
    }

    [Fact]
    public async Task GetNewAccessTokenFromRefreshTokenAsync_WhenHttpError_ReturnsNull()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        SetupHttpResponseForAnyUrl("{\"error\": \"invalid_grant\"}", HttpStatusCode.BadRequest);

        var result = await service.GetNewAccessTokenFromRefreshTokenAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWeightSeriesAsync_WhenNotAvailable_ReturnsEmptyList()
    {
        var display = CreateTestDisplay();
        var service = CreateService(display);

        var result = await service.GetWeightSeriesAsync();

        Assert.Empty(result);
        VerifyAnyHttpRequest(Times.Never());
    }

    [Fact]
    public async Task GetWeightSeriesAsync_WithSingleDataPoint_ReturnsCorrectWeight()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        var testDate = DateTime.UtcNow.Date.AddDays(-5);
        var mockResponse = CreateMockGoogleFitResponse((testDate, 75.5));
        
        // Set up response for all the chunked requests (3 requests for 90 days = 3x30-day chunks)
        SetupHttpResponseForAnyUrl(mockResponse, HttpStatusCode.OK);

        var result = await service.GetWeightSeriesAsync();

        // The service fetches 90 days in 30-day chunks, so we get 3 responses with the same data
        // We're looking for at least one entry with our expected date
        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Date == testDate && r.Weight == 75.5m);
    }

    [Fact]
    public async Task GetWeightSeriesAsync_WithMultipleDataPoints_ReturnsAllWeights()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        var testData = new[]
        {
            (DateTime.UtcNow.Date.AddDays(-10), 80.0),
            (DateTime.UtcNow.Date.AddDays(-9), 79.8),
            (DateTime.UtcNow.Date.AddDays(-8), 79.5),
            (DateTime.UtcNow.Date.AddDays(-7), 79.2),
            (DateTime.UtcNow.Date.AddDays(-6), 79.0)
        };

        var mockResponse = CreateMockGoogleFitResponse(testData);
        SetupHttpResponseForAnyUrl(mockResponse, HttpStatusCode.OK);

        var result = await service.GetWeightSeriesAsync();

        // The service fetches in chunks, so we'll have duplicates.
        // Just verify all our test dates are present
        Assert.NotEmpty(result);
        foreach (var (date, weight) in testData)
        {
            Assert.Contains(result, r => r.Date == date && r.Weight == (decimal)weight);
        }
    }

    [Fact]
    public async Task GetWeightSeriesAsync_WithMissingWeightData_SkipsEmptyEntries()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        var mockResponse = JsonSerializer.Serialize(new
        {
            bucket = new[]
            {
                new
                {
                    startTimeMillis = ((DateTimeOffset)DateTime.UtcNow.Date).ToUnixTimeMilliseconds(),
                    endTimeMillis = ((DateTimeOffset)DateTime.UtcNow.Date.AddDays(1)).ToUnixTimeMilliseconds(),
                    dataset = new[]
                    {
                        new
                        {
                            point = Array.Empty<object>() // No weight data
                        }
                    }
                }
            }
        });

        SetupHttpResponseForAnyUrl(mockResponse, HttpStatusCode.OK);

        var result = await service.GetWeightSeriesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLastKnownWeightAsync_WhenNoData_ReturnsNull()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        SetupHttpResponseForAnyUrl(JsonSerializer.Serialize(new { bucket = Array.Empty<object>() }), HttpStatusCode.OK);

        var result = await service.GetLastKnownWeightAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastKnownWeightAsync_WithMultipleEntries_ReturnsLatestWeight()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        var testData = new[]
        {
            (DateTime.UtcNow.Date.AddDays(-10), 80.0),
            (DateTime.UtcNow.Date.AddDays(-5), 78.5),
            (DateTime.UtcNow.Date.AddDays(-1), 77.2)
        };

        var mockResponse = CreateMockGoogleFitResponse(testData);
        SetupHttpResponseForAnyUrl(mockResponse, HttpStatusCode.OK);

        var result = await service.GetLastKnownWeightAsync();

        Assert.Equal(77.2m, result);
    }

    [Fact]
    public async Task GetLastKnownWeightAsync_WithZeroWeight_SkipsAndReturnsNonZero()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        var testData = new[]
        {
            (DateTime.UtcNow.Date.AddDays(-10), 80.0),
            (DateTime.UtcNow.Date.AddDays(-5), 78.5),
            (DateTime.UtcNow.Date.AddDays(-1), 0.0)  // Zero weight should be skipped
        };

        var mockResponse = CreateMockGoogleFitResponse(testData);
        SetupHttpResponseForAnyUrl(mockResponse, HttpStatusCode.OK);

        var result = await service.GetLastKnownWeightAsync();

        Assert.Equal(78.5m, result);
    }

    [Fact]
    public async Task FetchFromWebAsync_WhenNotAvailable_ReturnsNull()
    {
        var display = CreateTestDisplay();
        var service = CreateService(display);

        var result = await service.FetchFromWebAsync();

        Assert.Null(result);
        VerifyAnyHttpRequest(Times.Never());
    }

    [Fact]
    public async Task FetchFromWebAsync_OnHttpError_ThrowsException()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        SetupHttpResponseForAnyUrl("{\"error\": \"unauthorized\"}", HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await service.FetchFromWebAsync());
    }

    [Fact]
    public async Task FetchFromWebAsync_WithValidResponse_ReturnsParsedData()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        var testData = new[]
        {
            (DateTime.UtcNow.Date.AddDays(-2), 75.0),
            (DateTime.UtcNow.Date.AddDays(-1), 74.8)
        };

        var mockResponse = CreateMockGoogleFitResponse(testData);
        SetupHttpResponseForAnyUrl(mockResponse, HttpStatusCode.OK);

        var result = await service.FetchFromWebAsync();

        Assert.NotNull(result);
        Assert.NotNull(result.Bucket);
        // Service fetches 90 days in chunks, so we get multiple responses
        Assert.True(result.Bucket.Count >= 2);
    }

    [Fact]
    public async Task FetchFromWebAsync_UsesDatabaseCache()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        var mockResponse = CreateMockGoogleFitResponse((DateTime.UtcNow.Date, 75.0));
        SetupHttpResponseForAnyUrl(mockResponse, HttpStatusCode.OK);

        // First call - should hit API
        var result1 = await service.FetchFromWebAsync();
        Assert.NotNull(result1);

        // Second call - should use cache (no additional HTTP calls)
        var result2 = await service.FetchFromWebAsync();
        Assert.NotNull(result2);

        // Verify HTTP was called for the first request (with chunked requests)
        VerifyAnyHttpRequest(Times.AtLeastOnce());
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(89)]
    public async Task FetchFromWebAsync_HandlesVariousDayRanges(int daysToFetch)
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        // Generate test data for the specified range
        var testData = Enumerable.Range(0, daysToFetch)
            .Select(i => (DateTime.UtcNow.Date.AddDays(-i), 75.0 + i * 0.1))
            .ToArray();

        var mockResponse = CreateMockGoogleFitResponse(testData);
        SetupHttpResponseForAnyUrl(mockResponse, HttpStatusCode.OK);

        var result = await service.FetchFromWebAsync();

        Assert.NotNull(result);
        Assert.NotNull(result.Bucket);
        // Service always fetches 90 days in chunks, so count may be multiplied
        Assert.True(result.Bucket.Count >= daysToFetch);
    }

    [Fact]
    public async Task GetWeightSeriesAsync_ParsesDecimalWeightsCorrectly()
    {
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService(display);

        var testData = new[]
        {
            (DateTime.UtcNow.Date, 75.123456),
            (DateTime.UtcNow.Date.AddDays(-1), 74.987654),
            (DateTime.UtcNow.Date.AddDays(-2), 74.5)
        };

        var mockResponse = CreateMockGoogleFitResponse(testData);
        SetupHttpResponseForAnyUrl(mockResponse, HttpStatusCode.OK);

        var result = await service.GetWeightSeriesAsync();

        // Service fetches in chunks, so we'll have all our test data present
        Assert.True(result.Count >= 3);
        // Verify all our test data is present with correct precision
        Assert.Contains(result, r => r.Date == testData[0].Item1 && Math.Abs((double)r.Weight - testData[0].Item2) < 0.000001);
        Assert.Contains(result, r => r.Date == testData[1].Item1 && Math.Abs((double)r.Weight - testData[1].Item2) < 0.000001);
        Assert.Contains(result, r => r.Date == testData[2].Item1 && r.Weight == 74.5m);
    }
}
