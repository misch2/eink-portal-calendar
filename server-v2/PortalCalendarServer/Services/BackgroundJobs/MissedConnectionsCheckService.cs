using PortalCalendarServer.Data;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.BackgroundJobs;

/// <summary>
/// Background service that periodically checks for displays that have missed their expected connection times.
/// Runs every 5 minutes to detect frozen or empty battery displays.
/// Based on check_missed_connects from Perl code.
/// </summary>
public class MissedConnectionsCheckService : BackgroundService
{
    private readonly ILogger<MissedConnectionsCheckService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public MissedConnectionsCheckService(
        ILogger<MissedConnectionsCheckService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Missed Connections Check Service started (checking every {Interval} minutes)", _checkInterval.TotalMinutes);

        // Wait a bit before first check to let the app fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckMissedConnectionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking missed connections");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Missed Connections Check Service stopped");
    }

    private async Task CheckMissedConnectionsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking for missed connections (frozen or empty battery displays)");

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var displayService = scope.ServiceProvider.GetRequiredService<IDisplayService>();
        var context = scope.ServiceProvider.GetRequiredService<CalendarContext>();
        var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();

        var now = DateTime.UtcNow; // Same timezone as _last_visit

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

                // Calculate when the display should have connected based on last visit
                var timeZone = displayService.GetTimeZoneInfo(display);
                var lastVisitLocal = TimeZoneInfo.ConvertTimeFromUtc(lastVisit.Value, timeZone); // FIXME is this OK? The date is alread in the TZ or not?

                var wakeupInfo = displayService.GetNextWakeupTime(display, lastVisitLocal);
                var nextExpectedTime = wakeupInfo.NextWakeup;

                // Prevent false failures when the next expected time is very close to now
                // The clock in displays is not precise and this shouldn't be considered a missed connection
                var safetyLagMinutes = displayService.GetConfigInt(display, "alive_check_safety_lag_minutes") ?? 0;
                var nowSafe = now.AddMinutes(-safetyLagMinutes);

                if (nextExpectedTime < nowSafe)
                {
                    // Display missed its connection
                    displayService.IncreaseMissedConnectsCount(display, nextExpectedTime);

                    var missedConnects = displayService.GetMissedConnects(display);
                    var minFailures = displayService.GetConfigInt(display, "alive_check_minimal_failure_count") ?? 1;

                    if (missedConnects == minFailures)
                    {
                        // Just reached the threshold, send notification
                        displayService.SetConfig(display, "_frozen_notification_sent", "1");
                        await context.SaveChangesAsync(cancellationToken);

                        var message = FormatFrozenNotification(
                            display,
                            lastVisitLocal,
                            nextExpectedTime,
                            now,
                            minFailures);

                        _logger.LogWarning("Display {DisplayId} ({DisplayName}) is frozen: {Message}",
                            display.Id, display.Name, message);

                        // Send Telegram notification if configured
                        if (displayService.GetConfigBool(display, "telegram"))
                        {
                            var apiKey = displayService.GetConfig(display, "telegram_api_key");
                            var chatId = displayService.GetConfig(display, "telegram_chat_id");

                            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(chatId))
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
        PortalCalendarServer.Models.Entities.Display display,
        DateTime lastVisit,
        DateTime nextExpectedTime,
        DateTime now,
        int minFailures)
    {
        var hoursSince = (int)((now - lastVisit.ToUniversalTime()).TotalHours + 0.5);

        return $"Display '{display.Name}' (ID: {display.Id}) appears to be frozen.\n" +
               $"Last contact: {lastVisit:yyyy-MM-dd HH:mm} ({hoursSince} hours ago)\n" +
               $"Expected contact: {nextExpectedTime:yyyy-MM-dd HH:mm}\n" +
               $"Missed connections threshold: {minFailures}\n" +
               $"This could indicate a frozen display or empty battery.";
    }
}
