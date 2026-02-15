using System.Collections.Concurrent;
using System.Threading.Channels;

namespace PortalCalendarServer.Services.BackgroundJobs;

/// <summary>
/// Background service that processes image regeneration requests asynchronously.
/// Ensures only one active task per display (deduplication).
/// </summary>
public class ImageRegenerationService : BackgroundService
{
    private readonly ILogger<ImageRegenerationService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Channel<ImageRegenerationRequest> _channel;
    private readonly ConcurrentDictionary<string, DateTime> _activeRequests;

    public ImageRegenerationService(
        ILogger<ImageRegenerationService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;

        // Unbounded channel for requests
        _channel = Channel.CreateUnbounded<ImageRegenerationRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _activeRequests = new ConcurrentDictionary<string, DateTime>();
    }

    /// <summary>
    /// Enqueue a request to regenerate an image for a display.
    /// If a request for the same display is already active, this will be ignored.
    /// </summary>
    public bool EnqueueRequest(int displayId)
    {
        var request = new ImageRegenerationRequest { DisplayId = displayId };
        var key = request.GetKey();

        // Check if already processing this display
        if (_activeRequests.ContainsKey(key))
        {
            _logger.LogDebug("Image regeneration for display {DisplayId} is already in progress, skipping", displayId);
            return false;
        }

        // Try to write to channel
        if (_channel.Writer.TryWrite(request))
        {
            _logger.LogInformation("Enqueued image regeneration request for display {DisplayId}", displayId);
            return true;
        }

        _logger.LogWarning("Failed to enqueue image regeneration request for display {DisplayId}", displayId);
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Image Regeneration Service started");

        await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessRequestAsync(request, stoppingToken);
        }

        _logger.LogInformation("Image Regeneration Service stopped");
    }

    private async Task ProcessRequestAsync(ImageRegenerationRequest request, CancellationToken cancellationToken)
    {
        var key = request.GetKey();

        // Mark as active (with timestamp for potential cleanup)
        if (!_activeRequests.TryAdd(key, request.RequestedAt))
        {
            _logger.LogDebug("Request {Key} is already being processed", key);
            return;
        }

        try
        {
            _logger.LogInformation("Starting image regeneration for display {DisplayId}", request.DisplayId);

            using var scope = _serviceScopeFactory.CreateScope();
            var pageGeneratorService = scope.ServiceProvider.GetRequiredService<PageGeneratorService>();
            var displayService = scope.ServiceProvider.GetRequiredService<IDisplayService>();

            var display = displayService.GetDisplayById(request.DisplayId);
            if (display == null)
            {
                _logger.LogWarning("Display {DisplayId} not found, cannot regenerate image", request.DisplayId);
                return;
            }

            // Generate the image
            await pageGeneratorService.GenerateImageFromWebAsync(display);

            _logger.LogInformation(
                "Successfully regenerated image for display {DisplayId} (took {Duration}ms)",
                request.DisplayId,
                (DateTime.UtcNow - request.RequestedAt).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating image for display {DisplayId}", request.DisplayId);
        }
        finally
        {
            // Remove from active requests
            _activeRequests.TryRemove(key, out _);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Image Regeneration Service is stopping");

        // Complete the channel to stop accepting new requests
        _channel.Writer.Complete();

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the number of active regeneration tasks
    /// </summary>
    public int GetActiveTaskCount() => _activeRequests.Count;

    /// <summary>
    /// Checks if a regeneration is in progress for a specific display
    /// </summary>
    public bool IsRegenerating(int displayId)
    {
        var key = $"regenerate_{displayId}";
        return _activeRequests.ContainsKey(key);
    }
}
