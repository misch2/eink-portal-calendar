using System.Collections.Concurrent;

namespace PortalCalendarServer.Services.BackgroundJobs.Periodic;

/// <summary>
/// Background service that pre-generates display bitmaps around each display's
/// scheduled wakeup time. Checks every minute (configurable) and enqueues regeneration
/// for displays whose next wakeup is within the lead time (default 2 minutes before)
/// or whose most recent wakeup occurred within the trailing period (default 5 minutes after).
/// </summary>
public class BitmapGenerationService : PeriodicBackgroundService
{
    private readonly ILogger<BitmapGenerationService> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _startupDelay;
    private readonly TimeSpan _preGenerationLead;
    private readonly TimeSpan _postWakeupTrailing;

    /// <summary>
    /// Tracks which wakeup time we last pre-generated for each display,
    /// so we don't re-enqueue the same slot.
    /// </summary>
    private readonly ConcurrentDictionary<int, DateTime> _lastPreGeneratedWakeup = new();

    /// <summary>
    /// Tracks the next expected wakeup for each display so we can detect
    /// when it has just passed (i.e. the post-wakeup window).
    /// </summary>
    private readonly ConcurrentDictionary<int, DateTime> _nextExpectedWakeup = new();

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

        var trailingMinutes = configuration.GetValue<int>("BackgroundJobs:BitmapGeneration:PostWakeupGenerationTrailingMinutes");
        _postWakeupTrailing = TimeSpan.FromMinutes(trailingMinutes > 0 ? trailingMinutes : 5);
    }

    protected override TimeSpan Interval => _interval;

    protected override TimeSpan StartupDelay => _startupDelay;

    protected override string ServiceName => "Bitmap Generation Service";

    protected override async Task ExecuteWorkAsync(CancellationToken cancellationToken)
    {
        await using var scope = ServiceScopeFactory.CreateAsyncScope();
        var displayService = scope.ServiceProvider.GetRequiredService<IDisplayService>();

        var now = DateTime.UtcNow;
        var displays = displayService.GetAllDisplays().Where(d => !d.IsDefault()).ToList();

        foreach (var display in displays)
        {
            try
            {
                var fullDisplay = displayService.GetDisplayById(display.Id);
                var wakeupInfo = displayService.GetNextWakeupTime(fullDisplay, now);
                var timeUntilWakeup = wakeupInfo.NextWakeup - now;

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
                    }
                }

                // --- Post-wakeup window: generate AFTER a wakeup we previously tracked ---
                // If the next wakeup jumped forward compared to what we last tracked,
                // the previously expected wakeup has just passed.
                if (_nextExpectedWakeup.TryGetValue(display.Id, out var previouslyExpected)
                    && wakeupInfo.NextWakeup > previouslyExpected
                    && (now - previouslyExpected) <= _postWakeupTrailing
                    && !AlreadyGenerated(display.Id, previouslyExpected))
                {
                    _logger.LogInformation(
                        "Post-wakeup generating bitmap for display {DisplayId} — wakeup passed {Seconds:F0}s ago at {PassedWakeup}",
                        display.Id, (now - previouslyExpected).TotalSeconds, previouslyExpected.ToString("O"));

                    displayService.EnqueueImageRegenerationRequest(fullDisplay);
                    _lastPreGeneratedWakeup[display.Id] = previouslyExpected;
                }

                // Always track the current next wakeup for the next tick
                _nextExpectedWakeup[display.Id] = wakeupInfo.NextWakeup;
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