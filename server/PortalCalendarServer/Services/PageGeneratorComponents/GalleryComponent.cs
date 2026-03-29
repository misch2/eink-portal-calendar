using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

public class GalleryComponent(
    IDisplayService displayService,
    IGalleryService galleryService,
    IConfiguration configuration
    )
{
    public GalleryImageInfo? GetRandomImage(Display display)
    {
        var datetime = DateTime.UtcNow;
        var seed = int.Parse(datetime.ToString("ddHHmm"));  // seed changes every minute

        var galleryIdStr = displayService.GetConfig(display, "gallery_id");
        if (string.IsNullOrEmpty(galleryIdStr) || !int.TryParse(galleryIdStr, out var galleryId))
        {
            return null;
        }

        var gallery = galleryService.GetGalleryByIdAsync(galleryId).GetAwaiter().GetResult();
        if (gallery == null || gallery.Images.Count == 0)
        {
            return null;
        }

        var linksByImageId = gallery.ImageLinks.ToDictionary(l => l.GalleryImageId);

        var hiddenImageIds = gallery.ImageLinks
            .Where(l => l.IsHidden)
            .Select(l => l.GalleryImageId)
            .ToHashSet();

        var visibleImages = gallery.Images.Where(i => !hiddenImageIds.Contains(i.Id)).ToList();
        if (visibleImages.Count == 0)
        {
            return null;
        }

        var random = new Random(seed);
        var image = visibleImages[random.Next(visibleImages.Count)];

        var filePath = galleryService.GetImageFilePath(image);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var rotation = linksByImageId.TryGetValue(image.Id, out var link) ? link.Rotation : 0;

        return new GalleryImageInfo
        {
            GalleryId = galleryId,
            ImageId = image.Id,
            Description = image.Description,
            GalleryName = gallery.Name,
            Rotation = rotation
        };
    }
}

public class GalleryImageInfo
{
    public required int GalleryId { get; set; }
    public required int ImageId { get; set; }
    public string? Description { get; set; }
    public required string GalleryName { get; set; }
    public int Rotation { get; set; }

    /// <summary>
    /// Returns the relative URL to the image served by GalleriesController.ServeImage.
    /// </summary>
    public string ImageUrl => $"/galleries/{GalleryId}/images/{ImageId}";
}
