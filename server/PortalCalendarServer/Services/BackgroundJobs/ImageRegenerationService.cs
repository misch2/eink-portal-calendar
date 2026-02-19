using PortalCalendarServer.Models.POCOs;

namespace PortalCalendarServer.Services.BackgroundJobs;

/// <summary>
/// Background service that processes image regeneration requests asynchronously.
/// Ensures only one active task per display (deduplication).
/// </summary>
public class ImageRegenerationService : QueuedBackgroundService<ImageRegenerationRequest>
{
    private readonly ILogger<ImageRegenerationService> _logger;

    public ImageRegenerationService(
        ILogger<ImageRegenerationService> logger,
        IServiceScopeFactory serviceScopeFactory)
        : base(logger, serviceScopeFactory)
    {
        _logger = logger;
    }

    protected override string ServiceName => "Image Regeneration Service";

    protected override string GetRequestKey(ImageRegenerationRequest request) => request.GetKey();

    protected override DateTime GetRequestTimestamp(ImageRegenerationRequest request) => request.RequestedAt;

    /// <summary>
    /// Enqueue a request to regenerate an image for a display.
    /// If a request for the same display is already active, this will be ignored.
    /// </summary>
    public bool EnqueueRequest(int displayId)
    {
        var request = new ImageRegenerationRequest { DisplayId = displayId };
        return EnqueueRequest(request);
    }

    protected override async Task ProcessRequestAsync(ImageRegenerationRequest request, CancellationToken cancellationToken)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var pageGeneratorService = scope.ServiceProvider.GetRequiredService<PageGeneratorService>();
        var displayService = scope.ServiceProvider.GetRequiredService<IDisplayService>();

        var display = displayService.GetDisplayById(request.DisplayId);
        if (display == null)
        {
            _logger.LogWarning("Display {DisplayId} not found, cannot regenerate image", request.DisplayId);
            return;
        }

        await pageGeneratorService.GenerateImageFromWebAsync(display);
    }

    /// <summary>
    /// Checks if a regeneration is in progress for a specific display
    /// </summary>
    public bool IsRegenerating(int displayId)
    {
        var key = $"regenerate_{displayId}";
        return IsProcessing(key);
    }
}
