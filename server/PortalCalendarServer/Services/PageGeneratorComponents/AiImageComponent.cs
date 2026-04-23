using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

public class AiImageComponent(
    IDisplayService displayService,
    IGalleryService galleryService,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    IDatabaseCacheServiceFactory databaseCacheFactory,
    CalendarContext context,
    ILoggerFactory loggerFactory
    )
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<AiImageComponent>();

    public AiImageInfo? GetOrGenerateImage(Display display)
    {
        var apiKey = displayService.GetConfig(display, "aiimage_api_key");
        var prompt = displayService.GetConfig(display, "aiimage_prompt");

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(prompt))
            return null;

        var cacheHoursStr = displayService.GetConfig(display, "aiimage_cache_hours");
        var cacheHours = int.TryParse(cacheHoursStr, out var ch) && ch > 0 ? ch : 24;

        var quality = displayService.GetConfig(display, "aiimage_quality") ?? "standard";
        var style = displayService.GetConfig(display, "aiimage_style") ?? "natural";
        var size = PickSize(display);

        // Get or create the gallery for this display's AI images
        var galleryIdStr = displayService.GetConfig(display, "aiimage_gallery_id");
        int galleryId;
        if (string.IsNullOrEmpty(galleryIdStr) || !int.TryParse(galleryIdStr, out galleryId))
        {
            // No gallery configured — we can't auto-create from here (no write access to config)
            // so just generate and return as data URL without gallery storage
            return GenerateWithoutGallery(apiKey, prompt, size, quality, style);
        }

        var asOwner = galleryService.As(new ImpersonatedUserProvider(display.OwnerId));
        var gallery = asOwner.GetVisibleGalleryByIdAsync(galleryId).GetAwaiter().GetResult();
        if (gallery == null)
            return GenerateWithoutGallery(apiKey, prompt, size, quality, style);

        // Check if we have a recent-enough image in the gallery
        var latestImage = gallery.Images
            .OrderByDescending(i => i.UploadedAt)
            .FirstOrDefault();

        if (latestImage != null && latestImage.UploadedAt > DateTime.UtcNow.AddHours(-cacheHours))
        {
            _logger.LogDebug("Using cached AI image from gallery (uploaded {UploadedAt})", latestImage.UploadedAt);
            return new AiImageInfo
            {
                GalleryId = galleryId,
                ImageId = latestImage.Id,
                Description = latestImage.Description,
                ImageUrl = $"/galleries/{galleryId}/images/{latestImage.Id}"
            };
        }

        // Generate a new image
        var integrationService = new AiImageIntegrationService(
            loggerFactory.CreateLogger<AiImageIntegrationService>(),
            httpClientFactory,
            memoryCache,
            databaseCacheFactory,
            context,
            apiKey,
            prompt,
            size,
            quality,
            style
        );

        try
        {
            var result = integrationService.GenerateImageAsync().GetAwaiter().GetResult();

            // Save to gallery
            var description = result.RevisedPrompt ?? prompt;
            var savedImage = asOwner.AddImageFromBytesAsync(
                galleryId,
                result.ImageBytes,
                "ai-generated.png",
                "image/png",
                description
            ).GetAwaiter().GetResult();

            return new AiImageInfo
            {
                GalleryId = galleryId,
                ImageId = savedImage.Id,
                Description = description,
                ImageUrl = $"/galleries/{galleryId}/images/{savedImage.Id}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI image");

            // Fall back to most recent image in gallery if available
            if (latestImage != null)
            {
                _logger.LogInformation("Falling back to most recent gallery image");
                return new AiImageInfo
                {
                    GalleryId = galleryId,
                    ImageId = latestImage.Id,
                    Description = latestImage.Description,
                    ImageUrl = $"/galleries/{galleryId}/images/{latestImage.Id}"
                };
            }

            return null;
        }
    }

    private AiImageInfo? GenerateWithoutGallery(string apiKey, string prompt, string size, string quality, string style)
    {
        var integrationService = new AiImageIntegrationService(
            loggerFactory.CreateLogger<AiImageIntegrationService>(),
            httpClientFactory,
            memoryCache,
            databaseCacheFactory,
            context,
            apiKey,
            prompt,
            size,
            quality,
            style
        );

        try
        {
            var result = integrationService.GenerateImageAsync().GetAwaiter().GetResult();
            var base64 = Convert.ToBase64String(result.ImageBytes);

            return new AiImageInfo
            {
                Description = result.RevisedPrompt ?? prompt,
                ImageUrl = $"data:image/png;base64,{base64}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI image (no gallery fallback)");
            return null;
        }
    }

    /// <summary>
    /// Pick the best DALL-E size based on display aspect ratio.
    /// DALL-E 3 supports: 1024x1024, 1024x1792, 1792x1024
    /// </summary>
    private static string PickSize(Display display)
    {
        var w = display.VirtualWidth();
        var h = display.VirtualHeight();

        if (w == 0 || h == 0)
            return "1024x1024";

        var aspectRatio = (double)w / h;

        if (aspectRatio > 1.3)
            return "1792x1024"; // Landscape
        if (aspectRatio < 0.77)
            return "1024x1792"; // Portrait
        return "1024x1024";    // Square-ish
    }
}

public class AiImageInfo
{
    public int? GalleryId { get; set; }
    public int? ImageId { get; set; }
    public string? Description { get; set; }
    public required string ImageUrl { get; set; }
}
