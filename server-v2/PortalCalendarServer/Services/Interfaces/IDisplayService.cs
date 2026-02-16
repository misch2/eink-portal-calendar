using PortalCalendarServer.Models.ColorTypes;
using PortalCalendarServer.Models.Entities;
using System.Globalization;

namespace PortalCalendarServer.Services;

/// <summary>
/// Service interface for managing display configuration and settings
/// </summary>
public interface IDisplayService
{
    /// <summary>
    /// Get all displays in the system
    /// </summary>
    IEnumerable<Display> GetAllDisplays();

    /// <summary>
    /// Get a display by its ID
    /// </summary>
    Display? GetDisplayById(int displayNumber);

    /// <summary>
    /// Get the default display (ID = 0) which holds default configuration values
    /// </summary>
    Display GetDefaultDisplay();

    /// <summary>
    /// Get the timezone information for a specific display
    /// </summary>
    TimeZoneInfo GetTimeZoneInfo(Display display);

    /// <summary>
    /// Retrieves the date culture information associated with a specific display
    /// </summary>
    CultureInfo GetDateCultureInfo(Display display);

    /// <summary>
    /// Get configuration value for a display, with fallback to default display (ID = 0)
    /// </summary>
    string? GetConfig(Display display, string name);

    /// <summary>
    /// Get configuration value without checking defaults (only for this specific display)
    /// </summary>
    string? GetConfigWithoutDefaults(Display display, string name);

    /// <summary>
    /// Get configuration value from default display only (ID = 0)
    /// </summary>
    string? GetConfigDefaultsOnly(string name);

    /// <summary>
    /// Get configuration value as boolean
    /// </summary>
    bool GetConfigBool(Display display, string name, bool defaultValue = false);

    /// <summary>
    /// Get configuration value as integer
    /// </summary>
    int? GetConfigInt(Display display, string name);

    /// <summary>
    /// Get configuration value as double
    /// </summary>
    double? GetConfigDouble(Display display, string name);

    /// <summary>
    /// Set configuration value for a display
    /// </summary>
    void SetConfig(Display display, string name, string value);

    /// <summary>
    /// Get the color type implementation for a display
    /// </summary>
    IColorType? GetColorType(Display display);

    /// <summary>
    /// Enqueue a request to regenerate the display image for a display.
    /// </summary>
    /// <param name="display"></param>
    void EnqueueImageRegenerationRequest(Display display);

    /// <summary>
    /// Enqueue a request to regenerate the display image for all displays.
    /// </summary>
    void EnqueueAllImageRegenerationRequest();
}
