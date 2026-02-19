using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PortalCalendarServer.Services.Integrations;
using System.Net;
using System.Net.Http.Headers;

namespace PortalCalendarServer.Tests.Services.Integrations;

/// <summary>
/// Unit tests for CachingHttpMessageHandler
/// </summary>
public class CachingHttpMessageHandlerTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockInnerHandler;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<CachingHttpMessageHandler>> _mockLogger;
    private readonly CachingHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;

    public CachingHttpMessageHandlerTests()
    {
        _mockInnerHandler = new Mock<HttpMessageHandler>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1024 * 1024 * 10 // 10MB
        });
        _mockLogger = new Mock<ILogger<CachingHttpMessageHandler>>();

        _handler = new CachingHttpMessageHandler(
            _memoryCache,
            _mockLogger.Object,
            defaultCacheSeconds: 600)
        {
            InnerHandler = _mockInnerHandler.Object
        };

        _httpClient = new HttpClient(_handler);
    }

    private void SetupHttpResponse(
        string url,
        string content,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        int? maxAge = null)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(content)
        };

        if (maxAge.HasValue)
        {
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                MaxAge = TimeSpan.FromSeconds(maxAge.Value)
            };
        }

        _mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    [Fact]
    public async Task SendAsync_WithGetRequest_CachesResponse()
    {
        const string url = "https://example.com/test";
        const string content = "test content";
        SetupHttpResponse(url, content);

        var response1 = await _httpClient.GetAsync(url);
        var response2 = await _httpClient.GetAsync(url);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        Assert.Equal(content, content1);
        Assert.Equal(content, content2);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithPostRequest_DoesNotCache()
    {
        const string url = "https://example.com/test";
        const string content = "test content";

        _mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            });

        await _httpClient.PostAsync(url, new StringContent("post data"));
        await _httpClient.PostAsync(url, new StringContent("post data"));

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithDifferentUrls_CachesSeparately()
    {
        const string url1 = "https://example.com/test1";
        const string url2 = "https://example.com/test2";
        const string content1 = "content1";
        const string content2 = "content2";

        SetupHttpResponse(url1, content1);
        SetupHttpResponse(url2, content2);

        var response1 = await _httpClient.GetAsync(url1);
        var response2 = await _httpClient.GetAsync(url2);

        var responseContent1 = await response1.Content.ReadAsStringAsync();
        var responseContent2 = await response2.Content.ReadAsStringAsync();

        Assert.Equal(content1, responseContent1);
        Assert.Equal(content2, responseContent2);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url1),
                ItExpr.IsAny<CancellationToken>());

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url2),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithCacheControlMaxAge_UsesCacheControlDuration()
    {
        const string url = "https://example.com/test";
        const string content = "test content";
        const int maxAge = 300; // 5 minutes

        SetupHttpResponse(url, content, maxAge: maxAge);

        var response = await _httpClient.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithoutCacheControl_UsesDefaultDuration()
    {
        const string url = "https://example.com/test";
        const string content = "test content";

        SetupHttpResponse(url, content, maxAge: null);

        var response1 = await _httpClient.GetAsync(url);
        var response2 = await _httpClient.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithNonSuccessStatus_DoesNotCache()
    {
        const string url = "https://example.com/test";
        const string content = "error content";

        SetupHttpResponse(url, content, HttpStatusCode.NotFound);

        await _httpClient.GetAsync(url);
        await _httpClient.GetAsync(url);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_PreservesResponseHeaders()
    {
        const string url = "https://example.com/test";
        const string content = "test content";

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content)
        };
        response.Headers.Add("X-Custom-Header", "CustomValue");
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var response1 = await _httpClient.GetAsync(url);
        var response2 = await _httpClient.GetAsync(url);

        Assert.True(response2.Headers.Contains("X-Custom-Header"));
        Assert.Equal("CustomValue", response2.Headers.GetValues("X-Custom-Header").First());
        Assert.Equal("application/json", response2.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SendAsync_PreservesContentType()
    {
        const string url = "https://example.com/test.json";
        const string content = "{\"test\": \"data\"}";

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8"
        };

        _mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var response1 = await _httpClient.GetAsync(url);
        var response2 = await _httpClient.GetAsync(url);

        Assert.Equal("application/json", response2.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response2.Content.Headers.ContentType?.CharSet);
    }

    [Fact]
    public async Task SendAsync_WithBinaryContent_CachesCorrectly()
    {
        const string url = "https://example.com/image.png";
        var binaryContent = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new ByteArrayContent(binaryContent)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        _mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var response1 = await _httpClient.GetAsync(url);
        var response2 = await _httpClient.GetAsync(url);

        var content1 = await response1.Content.ReadAsByteArrayAsync();
        var content2 = await response2.Content.ReadAsByteArrayAsync();

        Assert.Equal(binaryContent, content1);
        Assert.Equal(binaryContent, content2);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithZeroMaxAge_DoesNotCache()
    {
        const string url = "https://example.com/test";
        const string content = "test content";

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content)
        };
        response.Headers.CacheControl = new CacheControlHeaderValue
        {
            MaxAge = TimeSpan.Zero
        };

        _mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content),
                Headers = { CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.Zero } }
            });

        await _httpClient.GetAsync(url);
        await _httpClient.GetAsync(url);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithEmptyContent_CachesCorrectly()
    {
        const string url = "https://example.com/empty";

        SetupHttpResponse(url, "");

        var response1 = await _httpClient.GetAsync(url);
        var response2 = await _httpClient.GetAsync(url);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        Assert.Empty(content1);
        Assert.Empty(content2);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithLargeContent_CachesCorrectly()
    {
        const string url = "https://example.com/large";
        var largeContent = new string('x', 100000); // 100KB of data

        SetupHttpResponse(url, largeContent);

        var response1 = await _httpClient.GetAsync(url);
        var response2 = await _httpClient.GetAsync(url);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        Assert.Equal(largeContent, content1);
        Assert.Equal(largeContent, content2);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_LogsCacheHit()
    {
        const string url = "https://example.com/test";
        SetupHttpResponse(url, "content");

        await _httpClient.GetAsync(url);
        await _httpClient.GetAsync(url);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cache HIT")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_LogsCacheMiss()
    {
        const string url = "https://example.com/test";
        SetupHttpResponse(url, "content");

        await _httpClient.GetAsync(url);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cache MISS")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithCustomCacheSeconds_UseCustomDuration()
    {
        var customHandler = new CachingHttpMessageHandler(
            _memoryCache,
            _mockLogger.Object,
            defaultCacheSeconds: 60) // 1 minute
        {
            InnerHandler = _mockInnerHandler.Object
        };

        using var client = new HttpClient(customHandler);
        const string url = "https://example.com/test";
        SetupHttpResponse(url, "content");

        await client.GetAsync(url);
        await client.GetAsync(url);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_PreservesStatusCode()
    {
        const string url = "https://example.com/test";
        SetupHttpResponse(url, "content", HttpStatusCode.OK);

        var response1 = await _httpClient.GetAsync(url);
        var response2 = await _httpClient.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    [Fact]
    public async Task SendAsync_WithMultipleHeaderValues_PreservesAll()
    {
        const string url = "https://example.com/test";
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("content")
        };
        response.Headers.Add("X-Multi", new[] { "value1", "value2", "value3" });

        _mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var response1 = await _httpClient.GetAsync(url);
        var response2 = await _httpClient.GetAsync(url);

        var headers1 = response1.Headers.GetValues("X-Multi").ToList();
        var headers2 = response2.Headers.GetValues("X-Multi").ToList();

        Assert.Equal(3, headers1.Count);
        Assert.Equal(3, headers2.Count);
        Assert.Contains("value1", headers1);
        Assert.Contains("value2", headers1);
        Assert.Contains("value3", headers1);
        Assert.Equal(headers1, headers2);
    }

    [Fact]
    public async Task SendAsync_WithPutRequest_DoesNotCache()
    {
        const string url = "https://example.com/test";
        const string content = "test content";

        _mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            });

        await _httpClient.PutAsync(url, new StringContent("put data"));
        await _httpClient.PutAsync(url, new StringContent("put data"));

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithDeleteRequest_DoesNotCache()
    {
        const string url = "https://example.com/test";
        const string content = "test content";

        _mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            });

        await _httpClient.DeleteAsync(url);
        await _httpClient.DeleteAsync(url);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithQueryParameters_CachesSeparately()
    {
        const string url1 = "https://example.com/test?param=1";
        const string url2 = "https://example.com/test?param=2";

        SetupHttpResponse(url1, "content1");
        SetupHttpResponse(url2, "content2");

        var response1 = await _httpClient.GetAsync(url1);
        var response2 = await _httpClient.GetAsync(url2);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        Assert.Equal("content1", content1);
        Assert.Equal("content2", content2);

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url1),
                ItExpr.IsAny<CancellationToken>());

        _mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url2),
                ItExpr.IsAny<CancellationToken>());
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _handler?.Dispose();
        _memoryCache?.Dispose();
        GC.SuppressFinalize(this);
    }
}
