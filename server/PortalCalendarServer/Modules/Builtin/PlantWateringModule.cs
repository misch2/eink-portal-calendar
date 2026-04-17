using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for the Plant Watering Monitor theme.
/// Owns config keys for up to 20 moisture sensor entity IDs and threshold settings.
/// No config tab — settings are part of the theme's custom config partial.
/// </summary>
public class PlantWateringModule : IPortalModule
{
    public const int MaxSensors = 30;

    public string ModuleId => "plantwatering";
    public string? ConfigTabDisplayName => null;
    public string? ConfigPartialView => null;

    public IReadOnlyList<string> OwnedConfigKeys { get; } = BuildConfigKeys();

    public IReadOnlyList<string> CheckboxConfigKeys => [];

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date) => null;

    private static List<string> BuildConfigKeys()
    {
        var keys = new List<string>
        {
            "plantwatering_threshold_dry",
            "plantwatering_threshold_wet",
            "plantwatering_name_override",
            "plantwatering_order_in_area",
            "plantwatering_history_days"
        };
        for (int i = 1; i <= MaxSensors; i++)
        {
            keys.Add($"plantwatering_sensor_{i}");
            keys.Add($"plantwatering_sensor_{i}_threshold_dry");
            keys.Add($"plantwatering_sensor_{i}_threshold_wet");
            keys.Add($"plantwatering_sensor_{i}_name_override");
            keys.Add($"plantwatering_sensor_{i}_order_in_area");
        }
        return keys;
    }
}
