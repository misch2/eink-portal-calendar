using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Modules.Builtin;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

public class GalleryComponent(
    IDisplayService displayService,
    IGalleryService galleryService
    )
{
    public GalleryImageInfo? GetRandomImage(Display display)
    {
        var datetime = DateTime.UtcNow;
        var seed = int.Parse(datetime.ToString("ddHHmm"));  // seed changes every minute

        var candidates = new List<GalleryCandidate>();

        for (int i = 1; i <= GalleryModule.MaxGalleries; i++)
        {
            var galleryIdStr = displayService.GetConfig(display, $"gallery_id_{i}");
            if (string.IsNullOrEmpty(galleryIdStr) || !int.TryParse(galleryIdStr, out var galleryId))
                continue;

            var ratioStr = displayService.GetConfig(display, $"gallery_preference_ratio_{i}");
            var ratio = double.TryParse(ratioStr, out var r) && r > 0 ? r : 1.0;

            var hideDescriptions = displayService.GetConfigBool(display, $"gallery_hide_descriptions_{i}");

            candidates.Add(new GalleryCandidate(galleryId, ratio, hideDescriptions));
        }

        if (candidates.Count == 0)
            return null;

        var random = new Random(seed);

        var selected = WeightedRandomSelector.Select(candidates, c => c.Ratio, random);
        if (selected == null)
            return null;

        var gallery = galleryService.GetGalleryByIdUnfilteredAsync(selected.GalleryId).GetAwaiter().GetResult();
        if (gallery == null || gallery.Images.Count == 0)
            return null;

        var linksByImageId = gallery.ImageLinks.ToDictionary(l => l.GalleryImageId);

        var hiddenImageIds = gallery.ImageLinks
            .Where(l => l.IsHidden)
            .Select(l => l.GalleryImageId)
            .ToHashSet();

        var visibleImages = gallery.Images.Where(i => !hiddenImageIds.Contains(i.Id)).ToList();
        if (visibleImages.Count == 0)
            return null;

        var image = visibleImages[random.Next(visibleImages.Count)];

        var filePath = galleryService.GetImageFilePath(image);
        if (!File.Exists(filePath))
            return null;

        var rotation = linksByImageId.TryGetValue(image.Id, out var link) ? link.Rotation : 0;

        return new GalleryImageInfo
        {
            GalleryId = selected.GalleryId,
            ImageId = image.Id,
            Description = image.Description,
            GalleryName = gallery.Name,
            Rotation = rotation,
            HideDescriptions = selected.HideDescriptions
        };
    }
}

public class GalleryCandidate(int galleryId, double ratio, bool hideDescriptions)
{
    public int GalleryId { get; } = galleryId;
    public double Ratio { get; } = ratio;
    public bool HideDescriptions { get; } = hideDescriptions;
}

public class GalleryImageInfo
{
    public required int GalleryId { get; set; }
    public required int ImageId { get; set; }
    public string? Description { get; set; }
    public required string GalleryName { get; set; }
    public int Rotation { get; set; }
    public bool HideDescriptions { get; set; }

    /// <summary>
    /// Returns the relative URL to the image served by GalleriesController.ServeImage.
    /// </summary>
    public string ImageUrl => $"/galleries/{GalleryId}/images/{ImageId}";
}
