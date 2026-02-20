using PortalCalendarServer.Data;
using PortalCalendarServer.Models.ColorTypes;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Models.POCOs.Board;
using PortalCalendarServer.Services.BackgroundJobs;
using System.Globalization;

namespace PortalCalendarServer.Services;

public class DisplayService(
    CalendarContext context,
    ILogger<DisplayService> logger,
    DisplayTypeRegistry displayTypeRegistry,
    ImageRegenerationService imageRegenerationService) : IDisplayService
{
    public IEnumerable<Display> GetAllDisplays()
    {
        return context.Displays
            .OrderBy(d => d.Id)
            .ToList();
    }

    public Display? GetDisplayById(int displayNumber)
    {
        var display = context.Displays.FirstOrDefault(d => d.Id == displayNumber);
        return display;
    }

    public Display GetDefaultDisplay()
    {
        return context.Displays.Single(d => d.Id == 0);
    }

    public TimeZoneInfo GetTimeZoneInfo(Display display)
    {
        var tzname = GetConfig(display, "timezone");
        if (tzname is null)
        {
            tzname = "UTC";
            logger.LogWarning("Timezone not set for display {DisplayId}, defaulting to {tzname}", display.Id, tzname);
        }
        return TimeZoneInfo.FindSystemTimeZoneById(tzname);
    }

    public CultureInfo GetDateCultureInfo(Display display)
    {
        var cultureName = GetConfig(display, "date_culture");
        if (cultureName is null)
        {
            logger.LogWarning("Date culture not set for display {DisplayId}, defaulting to invariant culture", display.Id);
            return CultureInfo.InvariantCulture;
        }
        return new CultureInfo(cultureName);
    }

    /// <summary>
    /// Get configuration value for a display, with fallback to default display (ID = 0)
    /// </summary>
    public string? GetConfig(Display display, string name)
    {
        // 1. real value (empty string usually means "unset" in HTML form)
        var value = GetConfigWithoutDefaults(display, name);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        // 2. default value (modifiable)
        var default_value = GetConfigDefaultsOnly(name);
        if (!string.IsNullOrEmpty(default_value))
        {
            return default_value;
        }

        return null;
    }

    /// <summary>
    /// Get configuration value without checking defaults (only for this specific display)
    /// </summary>
    public string? GetConfigWithoutDefaults(Display display, string name)
    {
        var config = display.Configs?.FirstOrDefault(c => c.Name == name);
        return config?.Value;
    }

    /// <summary>
    /// Get configuration value from default display only (ID = 0)
    /// </summary>
    public string? GetConfigDefaultsOnly(string name)
    {
        var defaultConfig = GetDefaultDisplay().Configs?.FirstOrDefault(c => c.Name == name);

        return defaultConfig?.Value;
    }

    /// <summary>
    /// Get configuration value as boolean
    /// </summary>
    public bool GetConfigBool(Display display, string name, bool defaultValue = false)
    {
        var value = GetConfig(display, name);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get configuration value as integer
    /// </summary>
    public int? GetConfigInt(Display display, string name)
    {
        var value = GetConfig(display, name);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (int.TryParse(value, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Get configuration value as double
    /// </summary>
    public double? GetConfigDouble(Display display, string name)
    {
        var value = GetConfig(display, name);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Set configuration value for a display
    /// </summary>
    public void SetConfig(Display display, string name, string value)
    {
        var config = context.Configs
            .FirstOrDefault(c => c.DisplayId == display.Id && c.Name == name);

        if (config != null)
        {
            config.Value = value;
            context.Update(config);
        }
        else
        {
            context.Configs.Add(new Config
            {
                DisplayId = display.Id,
                Name = name,
                Value = value
            });
        }
    }

    public IColorType? GetColorType(Display display)
    {
        if (string.IsNullOrWhiteSpace(display.DisplayType))
        {
            return null;
        }

        var ret = displayTypeRegistry.GetColorType(display.DisplayType);
        if (ret == null)
        {
            throw new InvalidOperationException($"Color type '{display.DisplayType}' not found in registry.");
        }

        return ret;
    }

    public void EnqueueImageRegenerationRequest(Display display)
    {
        // Enqueue image regeneration in background
        if (display.IsDefault())
        {
            logger.LogWarning("Skipping image regeneration request for default display (ID = 0)");
            return;
        }
        imageRegenerationService.EnqueueRequest(display.Id);
    }

    public void EnqueueAllImageRegenerationRequest()
    {
        var displays = GetAllDisplays().Where(d => !d.IsDefault()).ToList();
        foreach (var display in displays)
        {
            EnqueueImageRegenerationRequest(display);
        }
    }

    public int GetMissedConnects(Display display)
    {
        return GetConfigInt(display, "_missed_connects") ?? 0;
    }

    public DateTime? GetLastVisit(Display display)
    {
        var lastVisitStr = GetConfig(display, "_last_visit");
        if (string.IsNullOrEmpty(lastVisitStr))
        {
            return null;
        }

        if (DateTime.TryParse(lastVisitStr, null, DateTimeStyles.RoundtripKind, out var lastVisit))
        {
            return lastVisit.ToUniversalTime();
        }

        return null;
    }

    public decimal? GetVoltage(Display display)
    {
        var voltage = GetConfigDouble(display, "_last_voltage");
        if (!voltage.HasValue)
        {
            return null;
        }

        return Math.Round((decimal)voltage.Value, 2);
    }

    public decimal? GetBatteryPercent(Display display)
    {
        var min = GetConfigDouble(display, "_min_linear_voltage");
        var max = GetConfigDouble(display, "_max_linear_voltage");
        var voltage = GetVoltage(display);
        if (!min.HasValue || !max.HasValue || !voltage.HasValue)
        {
            return null;
        }

        var minDecimal = (decimal)min.Value;
        var maxDecimal = (decimal)max.Value;

        if (minDecimal == 0 || maxDecimal == 0)
        {
            return null;
        }

        var percentage = 100 * (voltage.Value - minDecimal) / (maxDecimal - minDecimal);
        percentage = Math.Max(0, Math.Min(100, percentage)); // Clip to 0-100

        return Math.Round(percentage, 1);
    }

    private DateTime GetNextWakeupTimeForDateTime(string schedule, DateTime dt, TimeZoneInfo timeZone)
    {
        // By default wake up tomorrow (truncate to day and add 1 day)
        var defaultDate = dt.Date.AddDays(1);

        // No schedule, wake up tomorrow (truncate to day and add 1 day)
        if (string.IsNullOrWhiteSpace(schedule))
        {
            return defaultDate;
        }

        // Crontab definitions are in the same time zone as the display
        // Parse cron schedule and find next occurrence
        try
        {
            var cronExpression = Cronos.CronExpression.Parse(schedule);
            var nextOccurrence = cronExpression.GetNextOccurrence(dt, timeZone);

            if (nextOccurrence.HasValue)
            {
                return nextOccurrence.Value;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid crontab schedule '{Schedule}', defaulting to tomorrow", schedule);
            return defaultDate;
        }

        // Fallback: wake up tomorrow if no next occurrence found
        logger.LogWarning("No next occurrence found for cron schedule '{Schedule}', defaulting to tomorrow", schedule);
        return defaultDate;
    }

    public WakeUpInfo GetNextWakeupTime(Display display, DateTime? optionalNow = null)
    {
        var now = optionalNow ?? DateTime.UtcNow;
        var timeZone = GetTimeZoneInfo(display);
        var schedule = GetConfig(display, "wakeup_schedule") ?? string.Empty;

        var nextWakeup = GetNextWakeupTimeForDateTime(schedule, now, timeZone);

        // If nextEvent is too close to now, it should return the next-next event
        // to avoid displays waking up multiple times due to inaccurate clocks
        var minSleepMinutes = GetConfigInt(display, "minimal_sleep_time_minutes") ?? 5;
        var diffSeconds = (nextWakeup - now).TotalSeconds;

        if (diffSeconds <= minSleepMinutes * 60)
        {
            nextWakeup = GetNextWakeupTimeForDateTime(schedule, nextWakeup.AddSeconds(1), timeZone);
        }

        var sleepInSeconds = (int)(nextWakeup - now).TotalSeconds;

        return new WakeUpInfo
        {
            NextWakeup = nextWakeup,
            SleepInSeconds = sleepInSeconds,
            Schedule = schedule
        };
    }

    public void ResetMissedConnectsCount(Display display)
    {
        SetConfig(display, "_missed_connects", "0");
        context.SaveChanges();
    }

    public void IncreaseMissedConnectsCount(Display display, DateTime expectedTimeOfConnect)
    {
        var previousExpectedTime = GetConfig(display, "_last_expected_time_of_connect");
        var expectedTimeStr = expectedTimeOfConnect.ToString("O");

        // Skip if this is the same expected time (avoid duplicate increments)
        if (previousExpectedTime == expectedTimeStr)
        {
            logger.LogDebug(
                "Skipping increase_missed_connects_count for display {DisplayId} because expected time hasn't changed",
                display.Id);
            return;
        }

        SetConfig(display, "_last_expected_time_of_connect", expectedTimeStr);

        var currentCount = GetMissedConnects(display);
        SetConfig(display, "_missed_connects", (currentCount + 1).ToString());

        context.SaveChanges();

        logger.LogWarning(
            "Increased missed connects count for display {DisplayId} to {Count} (expected at {ExpectedTime})",
            display.Id, currentCount + 1, expectedTimeOfConnect);
    }
}
