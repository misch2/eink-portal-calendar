using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Tests.TestData;
using System.Net;

namespace PortalCalendarServer.Tests.TestBase;

/// <summary>
/// Base class for integration service tests.
/// Provides common setup for mocking dependencies like HttpClient, DbContext, and IMemoryCache.
/// </summary>
public abstract class IntegrationServiceTestBase : IDisposable
{
    protected CalendarContext Context { get; }
    protected Mock<IHttpClientFactory> MockHttpClientFactory { get; }
    protected Mock<HttpMessageHandler> MockHttpMessageHandler { get; }
    protected Mock<IDatabaseCacheServiceFactory> MockDatabaseCacheServiceFactory { get; }
    protected IMemoryCache MemoryCache { get; }
    protected ILogger Logger { get; }
    
    // Pre-seeded test displays for common scenarios
    protected Display? TestDisplayBW { get; private set; }
    protected Display? TestDisplay3C { get; private set; }

    protected IntegrationServiceTestBase()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<CalendarContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        Context = new CalendarContext(options);

        // Setup mock HTTP message handler
        MockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Setup mock HTTP client factory
        MockHttpClientFactory = new Mock<IHttpClientFactory>();
        var httpClient = new HttpClient(MockHttpMessageHandler.Object);
        MockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Setup memory cache
        MemoryCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1024 * 1024 * 10 // 10MB
        });

        // Setup logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        Logger = loggerFactory.CreateLogger(GetType());

        // Setup mock database cache service factory
        MockDatabaseCacheServiceFactory = new Mock<IDatabaseCacheServiceFactory>();
        var databaseCache = new DatabaseCacheService(Context, loggerFactory.CreateLogger<DatabaseCacheService>(), "", TimeSpan.Zero);
        MockDatabaseCacheServiceFactory
            .Setup(f => f.Create(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(databaseCache);

        // Seed common test displays
        SeedCommonTestData();
    }

    /// <summary>
    /// Seeds common test data that all tests can use.
    /// Creates two standard displays: one BW and one 3C.
    /// </summary>
    protected virtual void SeedCommonTestData()
    {
        TestDisplayBW = CreateTestDisplay(
            TestDataHelper.Displays.BlackAndWhite.Name,
            TestDataHelper.Displays.BlackAndWhite.Mac,
            TestDataHelper.Displays.BlackAndWhite.Width,
            TestDataHelper.Displays.BlackAndWhite.Height,
            TestDataHelper.Displays.BlackAndWhite.ColorType);

        TestDisplay3C = CreateTestDisplay(
            TestDataHelper.Displays.ThreeColor.Name,
            TestDataHelper.Displays.ThreeColor.Mac,
            TestDataHelper.Displays.ThreeColor.Width,
            TestDataHelper.Displays.ThreeColor.Height,
            TestDataHelper.Displays.ThreeColor.ColorType);
    }

    /// <summary>
    /// Setup a mock HTTP response for the specified URL
    /// </summary>
    protected void SetupHttpResponse(string url, string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }

    /// <summary>
    /// Setup a mock HTTP response for any URL
    /// </summary>
    protected void SetupHttpResponseForAnyUrl(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }

    /// <summary>
    /// Setup a mock HTTP response that throws an exception
    /// </summary>
    protected void SetupHttpException(string url, Exception exception)
    {
        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
    }

    /// <summary>
    /// Create a test display entity
    /// </summary>
    protected Display CreateTestDisplay(
        string name = "TestDisplay",
        string mac = "aa:bb:cc:dd:ee:ff",
        int width = 800,
        int height = 480,
        string colorType = "BW")
    {
        var display = new Display
        {
            Name = name,
            Mac = mac,
            Width = width,
            Height = height,
            ColorType = colorType,
            Rotation = TestDataHelper.Displays.Common.DefaultRotation,
            Gamma = TestDataHelper.Displays.Common.DefaultGamma,
            BorderTop = TestDataHelper.Displays.Common.DefaultBorder,
            BorderRight = TestDataHelper.Displays.Common.DefaultBorder,
            BorderBottom = TestDataHelper.Displays.Common.DefaultBorder,
            BorderLeft = TestDataHelper.Displays.Common.DefaultBorder,
            Firmware = TestDataHelper.Displays.Common.DefaultFirmware,
            ThemeId = Models.Constants.Themes.DefaultId
        };

        Context.Displays.Add(display);
        Context.SaveChanges();

        return display;
    }

    /// <summary>
    /// Create a display with custom configuration values.
    /// Example: CreateDisplayWithConfigs(("key1", "value1"), ("key2", "value2"))
    /// </summary>
    protected Display CreateDisplayWithConfigs(params (string name, string value)[] configs)
    {
        var display = CreateTestDisplay();
        AddConfigsToDisplay(display, configs);
        return display;
    }

    /// <summary>
    /// Create a display with custom configuration values and specific display properties.
    /// </summary>
    protected Display CreateDisplayWithConfigs(
        string displayName,
        string mac,
        string colorType,
        params (string name, string value)[] configs)
    {
        var display = CreateTestDisplay(displayName, mac, colorType: colorType);
        AddConfigsToDisplay(display, configs);
        return display;
    }

    /// <summary>
    /// Add configuration entries to an existing display.
    /// </summary>
    protected void AddConfigsToDisplay(Display display, params (string name, string value)[] configs)
    {
        foreach (var (name, value) in configs)
        {
            Context.Configs.Add(new Config
            {
                DisplayId = display.Id,
                Name = name,
                Value = value
            });
        }

        Context.SaveChanges();
        Context.Entry(display).Collection(d => d.Configs).Load();
    }

    /// <summary>
    /// Create a display configured for Google Fit integration.
    /// </summary>
    protected Display CreateDisplayWithGoogleFitTokens(string? colorType = null)
    {
        var display = CreateTestDisplay(colorType: colorType ?? "BW");
        AddConfigsToDisplay(display, TestDataHelper.GoogleFit.StandardConfigs);
        return display;
    }

    /// <summary>
    /// Create a display configured for iCal integration.
    /// </summary>
    protected Display CreateDisplayWithICalConfig(string? url = null, string? colorType = null)
    {
        var display = CreateTestDisplay(colorType: colorType ?? "BW");
        AddConfigsToDisplay(display, TestDataHelper.ICal.StandardConfigs(url));
        return display;
    }

    /// <summary>
    /// Create a display configured for weather service.
    /// </summary>
    protected Display CreateDisplayWithWeatherConfig(
        string? latitude = null,
        string? longitude = null,
        string? colorType = null)
    {
        var display = CreateTestDisplay(colorType: colorType ?? "BW");
        AddConfigsToDisplay(display, TestDataHelper.Weather.StandardConfigs(latitude, longitude));
        return display;
    }

    /// <summary>
    /// Get the pre-seeded BW display.
    /// Returns the shared BW display instance or throws if not available.
    /// </summary>
    protected Display GetBWDisplay()
    {
        if (TestDisplayBW == null)
            throw new InvalidOperationException("BW test display not initialized");
        
        // Reload to ensure configs are loaded
        Context.Entry(TestDisplayBW).Collection(d => d.Configs).Load();
        return TestDisplayBW;
    }

    /// <summary>
    /// Get the pre-seeded 3C display.
    /// Returns the shared 3C display instance or throws if not available.
    /// </summary>
    protected Display Get3CDisplay()
    {
        if (TestDisplay3C == null)
            throw new InvalidOperationException("3C test display not initialized");
        
        // Reload to ensure configs are loaded
        Context.Entry(TestDisplay3C).Collection(d => d.Configs).Load();
        return TestDisplay3C;
    }

    /// <summary>
    /// Verify that an HTTP request was made
    /// </summary>
    protected void VerifyHttpRequest(string url, Times times)
    {
        MockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                times,
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Verify that any HTTP request was made
    /// </summary>
    protected void VerifyAnyHttpRequest(Times times)
    {
        MockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                times,
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    public virtual void Dispose()
    {
        Context?.Dispose();
        MemoryCache?.Dispose();
        GC.SuppressFinalize(this);
    }
}
