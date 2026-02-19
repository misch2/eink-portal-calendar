using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services.Caches;
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
    protected Display? TestDisplay { get; set; }

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
            Rotation = 0,
            Gamma = 2.2,
            BorderTop = 0,
            BorderRight = 0,
            BorderBottom = 0,
            BorderLeft = 0,
            Firmware = "1.0.0",
            ThemeId = Models.Constants.Themes.DefaultId
        };

        Context.Displays.Add(display);
        Context.SaveChanges();

        return display;
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
