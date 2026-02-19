namespace PortalCalendarServer.Services.BackgroundJobs.Periodic;

/// <summary>
/// Background service that periodically enqueues image regeneration requests for all active displays.
/// Runs every 15 minutes by default.
/// </summary>
public class BitmapGenerationService : PeriodicBackgroundService
{
    private readonly ILogger<BitmapGenerationService> _logger;
    private readonly TimeSpan _interval;

    public BitmapGenerationService(
        ILogger<BitmapGenerationService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
        : base(logger, serviceScopeFactory)
    {
        _logger = logger;

        var intervalMinutes = configuration.GetValue<int>("BackgroundJobs:BitmapGeneration:IntervalMinutes");
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override TimeSpan Interval => _interval;

    protected override string ServiceName => "Bitmap Generation Service";

    protected override async Task ExecuteWorkAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Enqueuing image regeneration requests for all active displays");

        await using var scope = ServiceScopeFactory.CreateAsyncScope();
        var displayService = scope.ServiceProvider.GetRequiredService<IDisplayService>();

        displayService.EnqueueAllImageRegenerationRequest();
        await Task.CompletedTask;
    }
}