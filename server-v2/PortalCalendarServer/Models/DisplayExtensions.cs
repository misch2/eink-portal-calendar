using System.Globalization;

namespace PortalCalendarServer.Models;

public static class DisplayExtensions
{
    public static bool IsDefault(this Display display)
    {
        return display.Id == 0;
    }

    public static string ColorTypeFormatted(this Display display)
    {
        return display.ColorType switch
        {
            "BW" => "Black & White",
            "4G" => "Grayscale, 4 levels",
            "3C" => "Black & White & Color (red or yellow)",
            _ => display.ColorType
        };
    }

    public static int MissedConnects(this Display display)
    {
        var missedStr = display.Configs?.FirstOrDefault(c => c.Name == "_missed_connects")?.Value;
        return int.TryParse(missedStr, out var missed) ? missed : 0;
    }

    public static DateTime? LastVisit(this Display display)
    {
        var lastVisitStr = display.Configs?.FirstOrDefault(c => c.Name == "_last_visit")?.Value;
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

    public static string GetConfigWithoutDefaults(this Display display, CalendarContext context, string name)
    {
        var config = display.Configs?.FirstOrDefault(c => c.Name == name);
        return config?.Value ?? string.Empty;
    }

    public static string GetConfigDefaultsOnly(this Display display, CalendarContext context, string name)
    {
        if (display.Id == 0)
        {
            return string.Empty;
        }

        // Get from default display (ID = 0)
        var defaultConfig = context.Configs
            .FirstOrDefault(c => c.DisplayId == 0 && c.Name == name);

        return defaultConfig?.Value ?? string.Empty;
    }

    public static string GetConfig(this Display display, CalendarContext context, string name, string defaultValue = "")
    {
        var config = display.Configs?.FirstOrDefault(c => c.Name == name);
        if (config != null)
        {
            return config.Value ?? defaultValue;
        }

        // Try to get from default display (ID = 0)
        var defaultConfig = context.Configs
            .FirstOrDefault(c => c.DisplayId == 0 && c.Name == name);

        return defaultConfig?.Value ?? defaultValue;
    }

    public static bool GetConfigBool(this Display display, string name, bool defaultValue = false)
    {
        var value = display.Configs?.FirstOrDefault(c => c.Name == name)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public static void SetConfig(this Display display, CalendarContext context, string name, string value)
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

    public static decimal? Voltage(this Display display)
    {
        var voltageStr = display.Configs?.FirstOrDefault(c => c.Name == "_last_voltage")?.Value;
        if (string.IsNullOrEmpty(voltageStr))
        {
            return null;
        }

        if (decimal.TryParse(voltageStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var voltage))
        {
            return Math.Round(voltage, 3);
        }

        return null;
    }

    public static decimal? BatteryPercent(this Display display)
    {
        var minStr = display.Configs?.FirstOrDefault(c => c.Name == "_min_linear_voltage")?.Value;
        var maxStr = display.Configs?.FirstOrDefault(c => c.Name == "_max_linear_voltage")?.Value;
        var voltage = display.Voltage();

        if (string.IsNullOrEmpty(minStr) || string.IsNullOrEmpty(maxStr) || !voltage.HasValue)
        {
            return null;
        }

        if (!decimal.TryParse(minStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var min) ||
            !decimal.TryParse(maxStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var max))
        {
            return null;
        }

        var percentage = 100 * (voltage.Value - min) / (max - min);
        percentage = Math.Max(0, Math.Min(100, percentage)); // Clip to 0-100

        return Math.Round(percentage, 1);
    }

    public static int NumColors(this Display display)
    {
        return display.ColorType switch
        {
            "BW" => 2,
            "4G" => 4,
            "3C" => 3,
            "16G" => 16,
            _ => 256
        };
    }

    public static (DateTime nextWakeup, int sleepInSeconds, string schedule) NextWakeupTime(this Display display)
    {
        var now = DateTime.UtcNow; // TODO: Use display timezone
        var schedule = display.Configs?.FirstOrDefault(c => c.Name == "wakeup_schedule")?.Value ?? string.Empty;

        DateTime nextWakeup;
        if (string.IsNullOrEmpty(schedule))
        {
            // No schedule, wake up tomorrow
            nextWakeup = now.Date.AddDays(1);
        }
        else
        {
            // TODO: Implement crontab schedule parsing
            // For now, default to next hour
            nextWakeup = now.AddHours(1).Date.AddHours(now.AddHours(1).Hour);
        }

        var sleepInSeconds = (int)(nextWakeup - now).TotalSeconds;

        return (nextWakeup, sleepInSeconds, schedule);
    }

    public static int VirtualWidth(this Display display)
    {
        return display.Rotation % 180 == 0 ? display.Width : display.Height;
    }

    public static int VirtualHeight(this Display display)
    {
        return display.Rotation % 180 == 0 ? display.Height : display.Width;
    }
}