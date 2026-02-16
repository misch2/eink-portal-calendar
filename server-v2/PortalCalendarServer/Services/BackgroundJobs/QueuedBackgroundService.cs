using System.Collections.Concurrent;
using System.Threading.Channels;

namespace PortalCalendarServer.Services.BackgroundJobs;

/// <summary>
/// Base class for background services that process requests from a queue (channel).
/// Supports deduplication to ensure only one active task per unique key.
/// </summary>
/// <typeparam name="TRequest">The type of request to process. Must provide a unique key for deduplication.</typeparam>
public abstract class QueuedBackgroundService<TRequest> : BackgroundService
    where TRequest : class
{
    private readonly ILogger _logger;
    protected readonly IServiceScopeFactory ServiceScopeFactory;
    private readonly Channel<TRequest> _channel;
    private readonly ConcurrentDictionary<string, DateTime> _activeRequests;

    protected QueuedBackgroundService(
        ILogger logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        ServiceScopeFactory = serviceScopeFactory;

        _channel = Channel.CreateUnbounded<TRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _activeRequests = new ConcurrentDictionary<string, DateTime>();
    }

    /// <summary>
    /// Gets the service name for logging purposes.
    /// </summary>
    protected abstract string ServiceName { get; }

    /// <summary>
    /// Gets a unique key for the request for deduplication purposes.
    /// </summary>
    protected abstract string GetRequestKey(TRequest request);

    /// <summary>
    /// Gets the timestamp when the request was created.
    /// </summary>
    protected abstract DateTime GetRequestTimestamp(TRequest request);

    /// <summary>
    /// Processes a single request. Implement the actual work logic in derived classes.
    /// </summary>
    protected abstract Task ProcessRequestAsync(TRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue a request for processing.
    /// If a request with the same key is already active, this will be ignored.
    /// </summary>
    public bool EnqueueRequest(TRequest request)
    {
        var key = GetRequestKey(request);

        // Check if already processing this request
        if (_activeRequests.ContainsKey(key))
        {
            _logger.LogDebug("{ServiceName}: Request {Key} is already in progress, skipping", ServiceName, key);
            return false;
        }

        // Try to write to channel
        if (_channel.Writer.TryWrite(request))
        {
            _logger.LogInformation("{ServiceName}: Enqueued request {Key}", ServiceName, key);
            return true;
        }

        _logger.LogWarning("{ServiceName}: Failed to enqueue request {Key}", ServiceName, key);
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{ServiceName} started", ServiceName);

        await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessRequestWithTrackingAsync(request, stoppingToken);
        }

        _logger.LogInformation("{ServiceName} stopped", ServiceName);
    }

    private async Task ProcessRequestWithTrackingAsync(TRequest request, CancellationToken cancellationToken)
    {
        var key = GetRequestKey(request);
        var requestedAt = GetRequestTimestamp(request);

        // Mark as active
        if (!_activeRequests.TryAdd(key, requestedAt))
        {
            _logger.LogDebug("{ServiceName}: Request {Key} is already being processed", ServiceName, key);
            return;
        }

        try
        {
            _logger.LogInformation("{ServiceName}: Starting processing request {Key}", ServiceName, key);

            var realStartTime = DateTime.UtcNow;

            await ProcessRequestAsync(request, cancellationToken);

            _logger.LogInformation(
                "{ServiceName}: Successfully processed request {Key} (took {Duration} ms since request, {RealDuration} ms real time)",
                ServiceName,
                key,
                (DateTime.UtcNow - requestedAt).TotalMilliseconds,
                (DateTime.UtcNow - realStartTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ServiceName}: Error processing request {Key}", ServiceName, key);
        }
        finally
        {
            // Remove from active requests
            _activeRequests.TryRemove(key, out _);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{ServiceName} is stopping", ServiceName);

        // Complete the channel to stop accepting new requests
        _channel.Writer.Complete();

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the number of active tasks being processed.
    /// </summary>
    public int GetActiveTaskCount() => _activeRequests.Count;

    /// <summary>
    /// Checks if a request with the given key is currently being processed.
    /// </summary>
    public bool IsProcessing(string key) => _activeRequests.ContainsKey(key);
}
