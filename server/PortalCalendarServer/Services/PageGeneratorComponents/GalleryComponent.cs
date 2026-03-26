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

        var visibleImages = gallery.Images.Where(i => !i.IsHidden).ToList();
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

        var imageBytes = File.ReadAllBytes(filePath);
        var base64 = Convert.ToBase64String(imageBytes);
        var dataUrl = $"data:{image.ContentType};base64,{base64}";

        return new GalleryImageInfo
        {
            ImageAsDataUrl = dataUrl,
            Description = image.Description,
            GalleryName = gallery.Name
        };
    }
}

public class GalleryImageInfo
{
    public required string ImageAsDataUrl { get; set; }
    public string? Description { get; set; }
    public required string GalleryName { get; set; }
}
