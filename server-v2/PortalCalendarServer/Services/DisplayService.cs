using PortalCalendarServer.Data;
using PortalCalendarServer.Models.ColorTypes;
using PortalCalendarServer.Models.Entities;

namespace PortalCalendarServer.Services;

public class DisplayService(
    CalendarContext context,
    ILogger<DisplayService> logger,
    ColorTypeRegistry colorTypeRegistry) : IDisplayService
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
        if (string.IsNullOrEmpty(display.ColorType))
        {
            return null;
        }

        var ret = colorTypeRegistry.GetColorType(display.ColorType);
        if (ret == null)
        {
            throw new InvalidOperationException($"Color type '{display.ColorType}' not found in registry.");
        }

        return ret;
    }
}
