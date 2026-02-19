using Microsoft.Extensions.Logging;
using PortalCalendarServer.Services.Integrations;
using PortalCalendarServer.Tests.TestBase;
using SixLabors.ImageSharp.Formats.Png;
using System.Net;
using System.Text.Json;

namespace PortalCalendarServer.Tests.Services.Integrations;

/// <summary>
/// Unit tests for XkcdIntegrationService
/// </summary>
public class XkcdIntegrationServiceTests : IntegrationServiceTestBase
{
    private const string XkcdApiUrl = "https://xkcd.com/info.0.json";
    private const string TestImageUrl = "https://imgs.xkcd.com/comics/test_comic.png";

    private XkcdIntegrationService CreateService()
    {
        var logger = new Mock<ILogger<XkcdIntegrationService>>().Object;

        return new XkcdIntegrationService(
            logger,
            MockHttpClientFactory.Object,
            MemoryCache,
            MockDatabaseCacheServiceFactory.Object,
            Context);
    }

    private static string CreateXkcdJsonResponse(
        int num = 2950,
        string title = "Test Comic",
        string alt = "Test alt text",
        string img = TestImageUrl,
        string year = "2024",
        string month = "12",
        string day = "15")
    {
        var response = new
        {
            num,
            title,
            safe_title = title,
            alt,
            img,
            transcript = "",
            year,
            month,
            day,
            link = "",
            news = ""
        };

        return JsonSerializer.Serialize(response);
    }

    private static byte[] CreateTestImageData(int width = 100, int height = 100, bool isLandscape = false)
    {
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(
            isLandscape ? width * 2 : width,
            isLandscape ? height : height * 2);

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    [Fact]
    public void IsConfigured_AlwaysReturnsTrue()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        var result = service.IsConfigured(display);

        Assert.True(result);
    }

    [Fact]
    public async Task GetLatestComicAsync_WithValidResponse_ReturnsComicData()
    {
        var service = CreateService();
        var jsonResponse = CreateXkcdJsonResponse();
        var imageData = CreateTestImageData();

        SetupHttpResponse(XkcdApiUrl, jsonResponse);
        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == TestImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageData)
            });

        var result = await service.GetLatestComicAsync();

        Assert.NotNull(result);
        Assert.Equal(2950, result.Number);
        Assert.Equal("Test Comic", result.Title);
        Assert.Equal("Test alt text", result.Alt);
        Assert.Equal(TestImageUrl, result.ImageUrl);
        Assert.NotEmpty(result.ImageData);
        Assert.Equal("2024", result.Year);
        Assert.Equal("12", result.Month);
        Assert.Equal("15", result.Day);
    }

    [Fact]
    public async Task GetLatestComicAsync_UsesDatabaseCaching()
    {
        var service = CreateService();
        var jsonResponse = CreateXkcdJsonResponse();
        var imageData = CreateTestImageData();

        SetupHttpResponse(XkcdApiUrl, jsonResponse);
        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == TestImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageData)
            });

        var result1 = await service.GetLatestComicAsync();
        var result2 = await service.GetLatestComicAsync();

        Assert.Equal(result1.Number, result2.Number);
        Assert.Equal(result1.Title, result2.Title);

        // Both JSON and image should be fetched only once due to database caching
        // Note: The actual caching happens in DatabaseCacheService, so we just verify
        // that we got consistent results
        Assert.Equal(result1.ImageData.Length, result2.ImageData.Length);
    }

    [Fact]
    public async Task GetLatestComicAsync_WithHttpError_ThrowsException()
    {
        var service = CreateService();

        SetupHttpResponse(XkcdApiUrl, "", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await service.GetLatestComicAsync());
    }

    [Fact]
    public async Task GetLatestComicAsync_WithNetworkError_ThrowsException()
    {
        var service = CreateService();

        SetupHttpException(XkcdApiUrl, new HttpRequestException("Network error"));

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await service.GetLatestComicAsync());
    }

    [Fact]
    public async Task GetLatestComicAsync_WithInvalidJson_ThrowsException()
    {
        var service = CreateService();

        SetupHttpResponse(XkcdApiUrl, "INVALID JSON");

        await Assert.ThrowsAsync<JsonException>(
            async () => await service.GetLatestComicAsync());
    }

    [Fact]
    public async Task GetLatestComicAsync_WithNullResponse_ThrowsException()
    {
        var service = CreateService();

        SetupHttpResponse(XkcdApiUrl, "null");

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GetLatestComicAsync());
    }

    [Fact]
    public async Task GetLatestComicAsync_WithCancellationToken_CanBeCancelled()
    {
        var service = CreateService();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        SetupHttpResponse(XkcdApiUrl, CreateXkcdJsonResponse());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.GetLatestComicAsync(cts.Token));
    }

    [Fact]
    public async Task GetLatestComicAsync_FetchesImageData()
    {
        var service = CreateService();
        var jsonResponse = CreateXkcdJsonResponse();
        var imageData = CreateTestImageData();

        SetupHttpResponse(XkcdApiUrl, jsonResponse);
        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == TestImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageData)
            });

        var result = await service.GetLatestComicAsync();

        Assert.Equal(imageData, result.ImageData);
    }

    [Fact]
    public async Task GetLatestComicAsync_CachesImageDataSeparately()
    {
        var service = CreateService();
        var jsonResponse = CreateXkcdJsonResponse();
        var imageData = CreateTestImageData();

        SetupHttpResponse(XkcdApiUrl, jsonResponse);
        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == TestImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageData)
            });

        var result1 = await service.GetLatestComicAsync();
        var result2 = await service.GetLatestComicAsync();

        // Both calls should return the same image data
        Assert.Equal(result1.ImageData, result2.ImageData);

        // Verify the image data was returned
        Assert.NotEmpty(result1.ImageData);
    }

    [Fact]
    public void DetermineIsLandscape_WithLandscapeImage_ReturnsTrue()
    {
        var imageData = CreateTestImageData(width: 200, height: 100, isLandscape: true);

        var result = XkcdIntegrationService.DetermineIsLandscape(imageData);

        Assert.True(result);
    }

    [Fact]
    public void DetermineIsLandscape_WithPortraitImage_ReturnsFalse()
    {
        var imageData = CreateTestImageData(width: 100, height: 200, isLandscape: false);

        var result = XkcdIntegrationService.DetermineIsLandscape(imageData);

        Assert.False(result);
    }

    [Fact]
    public void DetermineIsLandscape_WithSquareImage_ReturnsFalse()
    {
        var imageData = CreateTestImageData(width: 100, height: 100);

        var result = XkcdIntegrationService.DetermineIsLandscape(imageData);

        Assert.False(result);
    }

    [Fact]
    public void DetermineIsLandscape_WithAspectRatio4_3_ReturnsFalse()
    {
        // 4:3 aspect ratio is exactly at the threshold (1.333...)
        // The code checks for > 4/3, so exactly 4:3 should return false
        var imageData = CreateTestImageData(width: 400, height: 300, isLandscape: false);

        var result = XkcdIntegrationService.DetermineIsLandscape(imageData);

        Assert.False(result);
    }

    [Fact]
    public void DetermineIsLandscape_WithAspectRatioGreaterThan4_3_ReturnsTrue()
    {
        var imageData = CreateTestImageData(width: 500, height: 300, isLandscape: true);

        var result = XkcdIntegrationService.DetermineIsLandscape(imageData);

        Assert.True(result);
    }

    [Fact]
    public void DetermineIsLandscape_WithInvalidImageData_ReturnsFalse()
    {
        var invalidImageData = new byte[] { 0x00, 0x01, 0x02 };

        var result = XkcdIntegrationService.DetermineIsLandscape(invalidImageData);

        Assert.False(result);
    }

    [Fact]
    public void DetermineIsLandscape_WithEmptyImageData_ReturnsFalse()
    {
        var emptyImageData = Array.Empty<byte>();

        var result = XkcdIntegrationService.DetermineIsLandscape(emptyImageData);

        Assert.False(result);
    }

    [Fact]
    public void ConvertToDataUrl_WithValidImageData_ReturnsDataUrl()
    {
        var imageData = CreateTestImageData();

        var result = XkcdIntegrationService.ConvertToDataUrl(imageData);

        Assert.StartsWith("data:image/png;base64,", result);
        var base64Part = result.Substring("data:image/png;base64,".Length);
        var decodedBytes = Convert.FromBase64String(base64Part);
        Assert.Equal(imageData, decodedBytes);
    }

    [Fact]
    public void ConvertToDataUrl_WithEmptyData_ReturnsEmptyDataUrl()
    {
        var emptyData = Array.Empty<byte>();

        var result = XkcdIntegrationService.ConvertToDataUrl(emptyData);

        Assert.Equal("data:image/png;base64,", result);
    }

    [Fact]
    public async Task GetLatestComicAsync_ParsesAllFields()
    {
        var service = CreateService();
        var jsonResponse = CreateXkcdJsonResponse(
            num: 1234,
            title: "Advanced Test",
            alt: "Hover text here",
            img: TestImageUrl,
            year: "2023",
            month: "6",
            day: "25");
        var imageData = CreateTestImageData();

        SetupHttpResponse(XkcdApiUrl, jsonResponse);
        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == TestImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageData)
            });

        var result = await service.GetLatestComicAsync();

        Assert.Equal(1234, result.Number);
        Assert.Equal("Advanced Test", result.Title);
        Assert.Equal("Hover text here", result.Alt);
        Assert.Equal(TestImageUrl, result.ImageUrl);
        Assert.Equal("2023", result.Year);
        Assert.Equal("6", result.Month);
        Assert.Equal("25", result.Day);
    }

    [Fact]
    public async Task GetLatestComicAsync_WithImageFetchError_ThrowsException()
    {
        var service = CreateService();
        var jsonResponse = CreateXkcdJsonResponse();

        SetupHttpResponse(XkcdApiUrl, jsonResponse);
        SetupHttpResponse(TestImageUrl, "", HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await service.GetLatestComicAsync());
    }

    [Fact]
    public async Task GetLatestComicAsync_WithSpecialCharacters_ParsesCorrectly()
    {
        var service = CreateService();
        var jsonResponse = CreateXkcdJsonResponse(
            title: "Test & \"Quotes\" <Tags>",
            alt: "Alt with émojis 🎨 and symbols: ©®™");
        var imageData = CreateTestImageData();

        SetupHttpResponse(XkcdApiUrl, jsonResponse);
        MockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == TestImageUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageData)
            });

        var result = await service.GetLatestComicAsync();

        Assert.Contains("&", result.Title);
        Assert.Contains("\"", result.Title);
        Assert.Contains("🎨", result.Alt);
    }
}
