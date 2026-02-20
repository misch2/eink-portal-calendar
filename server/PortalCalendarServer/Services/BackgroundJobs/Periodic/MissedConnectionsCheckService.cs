using PortalCalendarServer.Data;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.BackgroundJobs.Periodic;

/// <summary>
/// Background service that periodically checks for displays that have missed their expected connection times.
/// Runs every 5 minutes to detect frozen or empty battery displays.
/// Based on check_missed_connects from Perl code.
/// </summary>
public class MissedConnectionsCheckService : PeriodicBackgroundService
{
    private readonly ILogger<MissedConnectionsCheckService> _logger;
    private readonly TimeSpan _interval;

    public MissedConnectionsCheckService(
        ILogger<MissedConnectionsCheckService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
        : base(logger, serviceScopeFactory)
    {
        _logger = logger;

        var intervalMinutes = configuration.GetValue<int>("BackgroundJobs:MissedConnections:IntervalMinutes");
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override TimeSpan Interval => _interval;

    protected override string ServiceName => "Missed Connections Check Service";

    protected override async Task ExecuteWorkAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking for missed connections (frozen or empty battery displays)");

        await using var scope = ServiceScopeFactory.CreateAsyncScope();
        var displayService = scope.ServiceProvider.GetRequiredService<IDisplayService>();
        var context = scope.ServiceProvider.GetRequiredService<CalendarContext>();
        var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();

        var now = DateTime.UtcNow;

        var displays = displayService.GetAllDisplays().Where(d => !d.IsDefault()).ToList();

        foreach (var display in displays)
        {
            try
            {
                var lastVisit = displayService.GetLastVisit(display);
                if (lastVisit == null)
                {
                    _logger.LogDebug("Display {DisplayId} has no last visit recorded, skipping", display.Id);
                    continue;
                }

                var wakeupInfo = displayService.GetNextWakeupTime(display, lastVisit.Value);
                var nextExpectedTime = wakeupInfo.NextWakeup;

                var safetyLagMinutes = displayService.GetConfigInt(display, "alive_check_safety_lag_minutes") ?? 0;
                var nowSafe = now.AddMinutes(-safetyLagMinutes);

                if (nextExpectedTime < nowSafe)
                {
                    displayService.IncreaseMissedConnectsCount(display, nextExpectedTime);

                    var missedConnects = displayService.GetMissedConnects(display);
                    var minFailures = displayService.GetConfigInt(display, "alive_check_minimal_failure_count") ?? 1;

                    if (missedConnects == minFailures)
                    {
                        displayService.SetConfig(display, "_frozen_notification_sent", "1");
                        await context.SaveChangesAsync(cancellationToken);

                        var message = FormatFrozenNotification(
                            display,
                            displayService.GetTimeZoneInfo(display),
                            lastVisit.Value,
                            nextExpectedTime,
                            now,
                            minFailures);

                        _logger.LogWarning("Display {DisplayId} ({DisplayName}) is frozen: {Message}",
                            display.Id, display.Name, message);

                        if (displayService.GetConfigBool(display, "telegram"))
                        {
                            var apiKey = displayService.GetConfig(display, "telegram_api_key");
                            var chatId = displayService.GetConfig(display, "telegram_chat_id");

                            if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(chatId))
                            {
                                try
                                {
                                    // TODO: Implement Telegram notification sending
                                    _logger.LogInformation("Would send Telegram notification to chat {ChatId} for display {DisplayId}",
                                        chatId, display.Id);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to send Telegram notification for display {DisplayId}", display.Id);
                                }
                            }
                        }
                    }
                    else if (missedConnects > minFailures)
                    {
                        _logger.LogDebug("Display {DisplayId} still frozen (missed {Count} connections)",
                            display.Id, missedConnects);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while checking missed connections for display {DisplayId}", display.Id);
            }
        }
    }

    private string FormatFrozenNotification(
        Models.DatabaseEntities.Display display,
        TimeZoneInfo timeZone,
        DateTime lastVisit,
        DateTime nextExpectedTime,
        DateTime now,
        int minFailures)
    {
        var hoursSince = (int)((now - lastVisit).TotalHours + 0.5);

        var lastVisitLocal = TimeZoneInfo.ConvertTimeFromUtc(lastVisit, timeZone);
        var nextExpectedLocal = TimeZoneInfo.ConvertTimeFromUtc(nextExpectedTime, timeZone);

        return $"Display '{display.Name}' (ID: {display.Id}) appears to be frozen.\n" +
               $"Last contact: {lastVisitLocal:yyyy-MM-dd HH:mm} ({hoursSince} hours ago)\n" +
               $"Expected contact: {nextExpectedLocal:yyyy-MM-dd HH:mm}\n" +
               $"Missed connections threshold: {minFailures}\n" +
               $"This could indicate a frozen display or empty battery.";
    }
}
