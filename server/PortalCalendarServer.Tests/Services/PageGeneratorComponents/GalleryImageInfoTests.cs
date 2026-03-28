using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Tests.Services.PageGeneratorComponents;

/// <summary>
/// Unit tests for GalleryImageInfo
/// </summary>
public class GalleryImageInfoTests
{
    [Theory]
    [InlineData(1, 10, "/galleries/1/images/10")]
    [InlineData(42, 99, "/galleries/42/images/99")]
    [InlineData(0, 0, "/galleries/0/images/0")]
    public void ImageUrl_ReturnsExpectedFormat(int galleryId, int imageId, string expectedUrl)
    {
        var info = new GalleryImageInfo
        {
            GalleryId = galleryId,
            ImageId = imageId,
            GalleryName = "test"
        };

        Assert.Equal(expectedUrl, info.ImageUrl);
    }
}
