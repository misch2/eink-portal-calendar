using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models;
using PortalCalendarServer.Models.ColorTypes;
using PortalCalendarServer.Models.Entities;
using System;

namespace PortalCalendarServer.Services;

public class DisplayService
{
    private readonly CalendarContext _context;
    private readonly ILogger<DisplayService> _logger;
    private readonly ColorTypeRegistry _colorTypeRegistry;

    private Display? _currentDisplay;
    private TimeZoneInfo? _timeZoneInfo;

    public DisplayService(
        CalendarContext context,
        ILogger<DisplayService> logger,
        ColorTypeRegistry colorTypeRegistry)
    {
        _context = context;
        _logger = logger;
        _colorTypeRegistry = colorTypeRegistry;
    }

    public IEnumerable<Display> GetAllDisplays()
    {
        return _context.Displays
            .Include(d => d.Configs)
            .OrderBy(d => d.Id)
            .ToList();
    }

    public Display GetDefaultDisplay()
    {
        return _context.Displays
            .Include(d => d.Configs)
            .Single(d => d.Id == 0);
    }

    public void UseDisplay(Display display)
    {
        _currentDisplay = display;
        _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(GetConfig("timezone") ?? "UTC");
    }

    /// <summary>
    /// Get the currently set display
    /// </summary>
    public Display? GetCurrentDisplay()
    {
        return _currentDisplay;
    }

    public TimeZoneInfo GetTimeZoneInfo()
    {
        ValidateDisplayIsSet();
        return _timeZoneInfo!;
    }

    private void ValidateDisplayIsSet()
    {
        if (_currentDisplay == null)
        {
            throw new InvalidOperationException("No display is currently set. Call UseDisplay() first.");
        }
    }

    /// <summary>
    /// Get configuration value for a display, with fallback to default display (ID = 0)
    /// </summary>
    public string? GetConfig(string name, string defaultValue = "")
    {
        ValidateDisplayIsSet();

        // 1. real value (empty string usually means "unset" in HTML form)
        var value = GetConfigWithoutDefaults(name);
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
    public string? GetConfigWithoutDefaults(string name)
    {
        ValidateDisplayIsSet();
        var config = _currentDisplay!.Configs?.FirstOrDefault(c => c.Name == name);
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
    public bool GetConfigBool(string name, bool defaultValue = false)
    {
        ValidateDisplayIsSet();

        var value = GetConfig(name);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Set configuration value for a display
    /// </summary>
    public void SetConfig(string name, string value)
    {
        ValidateDisplayIsSet();
        var config = _context.Configs
            .FirstOrDefault(c => c.DisplayId == _currentDisplay!.Id && c.Name == name);

        if (config != null)
        {
            config.Value = value;
            _context.Update(config);
        }
        else
        {
            _context.Configs.Add(new Config
            {
                DisplayId = _currentDisplay!.Id,
                Name = name,
                Value = value
            });
        }
    }

    /// <summary>
    /// Save all changes to the database
    /// </summary>
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public IColorType? GetColorType()
    {
        ValidateDisplayIsSet();

        if (string.IsNullOrEmpty(_currentDisplay!.ColorType))
        {
            return null;
        }

        var ret = _colorTypeRegistry.GetColorType(_currentDisplay!.ColorType);
        if (ret == null)
        {
            throw new InvalidOperationException($"Color type '{_currentDisplay.ColorType}' not found in registry.");
        }

        return ret;
    }

    public DateTime GetNowWithTimeZone()
    {
        ValidateDisplayIsSet();
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetTimeZoneInfo());
    }

}
