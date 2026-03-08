using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Models.POCOs;
using PortalCalendarServer.Models.POCOs.Bitmap;
using PortalCalendarServer.Models.POCOs.Board;
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
    /// Enqueue a request to regenerate the display image for a display.
    /// </summary>
    /// <param name="display"></param>
    void EnqueueImageRegenerationRequest(Display display);

    /// <summary>
    /// Enqueue a request to regenerate the display image for all displays.
    /// </summary>
    void EnqueueAllImageRegenerationRequest();

    /// <summary>
    /// Get the number of missed connection attempts for a display
    /// </summary>
    int GetMissedConnects(Display display);

    /// <summary>
    /// Get the last visit timestamp for a display
    /// </summary>
    DateTime? GetLastVisit(Display display);

    /// <summary>
    /// Get the battery voltage for a display
    /// </summary>
    decimal? GetVoltage(Display display);

    /// <summary>
    /// Get the battery percentage for a display
    /// </summary>
    decimal? GetBatteryPercent(Display display);

    /// <summary>
    /// Calculate the next wakeup time for a display based on its schedule. Returns data in display time zone.
    /// </summary>
    WakeUpInfo GetNextWakeupTime(Display display, DateTime? optionalNow = null);

    /// <summary>
    /// Reset the missed connections count for a display to zero
    /// </summary>
    void ResetMissedConnectsCount(Display display);

    /// <summary>
    /// Increase the missed connections count for a display
    /// </summary>
    void IncreaseMissedConnectsCount(Display display, DateTime expectedTimeOfConnect);

    /// <summary>
    /// Update the render information for a display, including the timestamp of when it was rendered and any errors that occurred during rendering.
    /// </summary>
    void UpdateRenderInfo(Display display, DateTime renderedAt, string? renderErrors);

    /// <summary>
    /// Get all available display types
    /// </summary>
    List<DisplayType> GetDisplayTypes();

    /// <summary>
    /// Get all available color variants
    /// </summary>
    List<ColorVariant> GetColorVariants();

    /// <summary>
    /// Get all available dithering types
    /// </summary>
    List<DitheringType> GetDitheringTypes();

    /// <summary>
    /// Generate bitmap image from a full color PNG snapshot
    /// </summary>
    BitmapResult ConvertExistingWebSnapshot(Display display, BitmapOptions options);

    string RawWebSnapshotFileName(Display display);

    /// <summary>
    /// Builds a <see cref="BitmapResult"/> for the given display using the supplied rendering options.
    /// Returns <c>null</c> when the display, its rendered bitmap, or its display-type information cannot be found;
    /// the <paramref name="errorMessage"/> out-parameter will contain a human-readable reason in that case.
    /// </summary>
    BitmapResult ConvertExistingRawBitmap(
        int displayId,
        OutputFormat format,
        DisplayRotation? rotate = null,
        string? flip = null);
}

// FIXME ConvertExistingRawBitmap vs  ConvertExistingWebSnapshot ???
