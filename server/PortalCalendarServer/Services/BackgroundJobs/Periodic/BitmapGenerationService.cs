using System.Collections.Concurrent;

namespace PortalCalendarServer.Services.BackgroundJobs.Periodic;

/// <summary>
/// Background service that pre-generates display bitmaps around each display's
/// scheduled wakeup time. Checks every minute (configurable) and enqueues regeneration
/// for displays whose next wakeup is within the lead time (default 2 minutes before).
/// Additionally, can periodically regenerate bitmaps on a fixed interval for all displays.
/// </summary>
public class BitmapGenerationService : PeriodicBackgroundService
{
    private readonly ILogger<BitmapGenerationService> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _startupDelay;
    private readonly TimeSpan _preGenerationLead;
    private readonly TimeSpan? _periodicGenerationInterval;

    /// <summary>
    /// Tracks which wakeup time we last pre-generated for each display,
    /// so we don't re-enqueue the same slot.
    /// </summary>
    private readonly ConcurrentDictionary<int, DateTime> _lastPreGeneratedWakeup = new();

    /// <summary>
    /// Tracks when we last performed a periodic generation for each display.
    /// </summary>
    private readonly ConcurrentDictionary<int, DateTime> _lastPeriodicGeneration = new();

    public BitmapGenerationService(
        ILogger<BitmapGenerationService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
        : base(logger, serviceScopeFactory)
    {
        _logger = logger;

        var checkMinutes = configuration.GetValue<int>("BackgroundJobs:BitmapGeneration:CheckIntervalMinutes");
        _interval = TimeSpan.FromMinutes(checkMinutes > 0 ? checkMinutes : 1);

        var startupDelayMinutes = configuration.GetValue<int>("BackgroundJobs:BitmapGeneration:StartupDelayMinutes");
        _startupDelay = TimeSpan.FromMinutes(startupDelayMinutes);

        var leadMinutes = configuration.GetValue<int>("BackgroundJobs:BitmapGeneration:PreWakeupGenerationLeadingMinutes");
        _preGenerationLead = TimeSpan.FromMinutes(leadMinutes > 0 ? leadMinutes : 2);

        var periodicMinutes = configuration.GetValue<int>("BackgroundJobs:BitmapGeneration:PeriodicGenerationIntervalMinutes");
        _periodicGenerationInterval = periodicMinutes > 0 ? TimeSpan.FromMinutes(periodicMinutes) : null;
    }

    protected override TimeSpan Interval => _interval;

    protected override TimeSpan StartupDelay => _startupDelay;

    protected override string ServiceName => "Bitmap Generation Service";

    protected override async Task ExecuteWorkAsync(CancellationToken cancellationToken)
    {
        await using var scope = ServiceScopeFactory.CreateAsyncScope();
        var displayService = scope.ServiceProvider.GetRequiredService<IDisplayService>();

        var now = DateTime.UtcNow;
        var displays = displayService.As(new ImpersonatedUserProvider(null)).GetVisibleDisplays().Where(d => !d.IsDefault()).ToList();

        foreach (var display in displays)
        {
            try
            {
                var fullDisplay = displayService.As(new ImpersonatedUserProvider(null)).GetVisibleDisplayById(display.Id);
                var wakeupInfo = displayService.GetNextWakeupTime(fullDisplay, now);
                var timeUntilWakeup = wakeupInfo.NextWakeup - now;
                var enqueued = false;

                // --- Pre-wakeup window: generate BEFORE the upcoming wakeup ---
                if (timeUntilWakeup <= _preGenerationLead && timeUntilWakeup > TimeSpan.Zero)
                {
                    if (!AlreadyGenerated(display.Id, wakeupInfo.NextWakeup))
                    {
                        _logger.LogInformation(
                            "Pre-generating bitmap for display {DisplayId} — wakeup in {Seconds:F0}s at {NextWakeup}",
                            display.Id, timeUntilWakeup.TotalSeconds, wakeupInfo.NextWakeup.ToString("O"));

                        displayService.EnqueueImageRegenerationRequest(fullDisplay);
                        _lastPreGeneratedWakeup[display.Id] = wakeupInfo.NextWakeup;
                        enqueued = true;
                    }
                }

                // --- Periodic generation: regenerate every N minutes regardless of wakeup schedule ---
                if (!enqueued && _periodicGenerationInterval is not null)
                {
                    var shouldGenerate = !_lastPeriodicGeneration.TryGetValue(display.Id, out var lastGenTime)
                                         || (now - lastGenTime) >= _periodicGenerationInterval.Value;

                    if (shouldGenerate)
                    {
                        _logger.LogInformation(
                            "Periodic bitmap generation for display {DisplayId} (interval: {IntervalMinutes} min)",
                            display.Id, _periodicGenerationInterval.Value.TotalMinutes);

                        displayService.EnqueueImageRegenerationRequest(fullDisplay);
                        _lastPeriodicGeneration[display.Id] = now;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pre-generation for display {DisplayId}", display.Id);
            }
        }
    }

    private bool AlreadyGenerated(int displayId, DateTime wakeupTime)
    {
        return _lastPreGeneratedWakeup.TryGetValue(displayId, out var lastWakeup)
               && lastWakeup == wakeupTime;
    }
}