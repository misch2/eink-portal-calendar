namespace PortalCalendarServer.Services.BackgroundJobs;

/// <summary>
/// Base class for background services that execute periodically at a fixed interval.
/// </summary>
public abstract class PeriodicBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    protected readonly IServiceScopeFactory ServiceScopeFactory;

    protected PeriodicBackgroundService(
        ILogger logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        ServiceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// Gets the interval between executions.
    /// </summary>
    protected abstract TimeSpan Interval { get; }

    /// <summary>
    /// Gets the optional startup delay before the first execution.
    /// Default is no delay.
    /// </summary>
    protected virtual TimeSpan StartupDelay => TimeSpan.Zero;

    /// <summary>
    /// Gets the service name for logging purposes.
    /// </summary>
    protected abstract string ServiceName { get; }

    /// <summary>
    /// Executes the periodic work. Implement this method in derived classes.
    /// </summary>
    protected abstract Task ExecuteWorkAsync(CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{ServiceName} started (checking every {Interval} minutes)",
            ServiceName, Interval.TotalMinutes);

        if (StartupDelay > TimeSpan.Zero)
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {ServiceName}", ServiceName);
            }

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("{ServiceName} stopped", ServiceName);
    }
}
