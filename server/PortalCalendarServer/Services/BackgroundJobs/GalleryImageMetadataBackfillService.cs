using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using SixLabors.ImageSharp;

namespace PortalCalendarServer.Services.BackgroundJobs;

/// <summary>
/// One-off startup service that backfills FileSize, Width, and Height
/// for existing gallery images that are missing these values.
/// </summary>
public class GalleryImageMetadataBackfillService(
    ILogger<GalleryImageMetadataBackfillService> logger,
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var galleryImagesPath = configuration["Paths:GalleryImages"]!;

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CalendarContext>();

        var images = await context.GalleryImages
            .Where(i => i.FileSize == null || i.Width == null || i.Height == null)
            .ToListAsync(stoppingToken);

        if (images.Count == 0)
        {
            logger.LogInformation("Gallery image metadata backfill: nothing to do");
            return;
        }

        logger.LogInformation("Gallery image metadata backfill: processing {Count} images", images.Count);

        var updated = 0;
        foreach (var image in images)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var filePath = Path.Combine(galleryImagesPath, image.PrimaryFolder, image.FileName);
            if (!File.Exists(filePath))
            {
                logger.LogWarning("Backfill: file not found for image {Id}: {Path}", image.Id, filePath);
                continue;
            }

            image.FileSize = new FileInfo(filePath).Length;

            try
            {
                var info = Image.Identify(filePath);
                if (info != null)
                {
                    image.Width = info.Width;
                    image.Height = info.Height;
                }
            }
            catch
            {
                logger.LogWarning("Backfill: could not read dimensions for image {Id}: {Path}", image.Id, filePath);
            }

            updated++;
        }

        await context.SaveChangesAsync(stoppingToken);
        logger.LogInformation("Gallery image metadata backfill: updated {Count} images", updated);
    }
}
