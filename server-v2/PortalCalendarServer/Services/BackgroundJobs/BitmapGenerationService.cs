namespace PortalCalendarServer.Services.BackgroundJobs;

/// <summary>
/// Background service that periodically enqueues image regeneration requests for all active displays.
/// Runs every 15 minutes by default.
/// </summary>
public class BitmapGenerationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BitmapGenerationService> _logger;
    private readonly TimeSpan _interval;

    public BitmapGenerationService(
        IServiceProvider serviceProvider,
        ILogger<BitmapGenerationService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Read interval from configuration, default to 15 minutes
        var intervalMinutes = configuration.GetValue<int?>("BitmapGeneration:IntervalMinutes") ?? 15;
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Bitmap Generation Service started. Will run every {Minutes} minutes.",
            _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await EnqueueRegenerationForAllDisplaysAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bitmap generation scheduling");
                // Continue running even if scheduling fails
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Bitmap Generation Service stopped.");
    }

    private async Task EnqueueRegenerationForAllDisplaysAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enqueuing image regeneration for all active displays...");

        try
        {
            // Create a scope to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var displayService = scope.ServiceProvider.GetRequiredService<IDisplayService>();
            var imageRegenerationService = scope.ServiceProvider.GetRequiredService<ImageRegenerationService>();

            // Skip the default display (ID = 0) as it's just a template
            var displays = displayService.GetAllDisplays().ToList().Where(d => !d.IsDefault()).ToList();
            var enqueuedCount = 0;

            foreach (var display in displays)
            {
                if (imageRegenerationService.EnqueueRequest(display.Id))
                {
                    enqueuedCount++;
                }
            }

            _logger.LogInformation(
                "Enqueued {EnqueuedCount} of {TotalCount} displays for image regeneration",
                enqueuedCount,
                displays.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue image regeneration requests");
            throw;
        }

        await Task.CompletedTask;
    }
}