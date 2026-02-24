using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Models.POCOs;

/// <summary>
/// View model passed to the <c>_ConfigCheckbox</c> and <c>_ConfigTextInput</c> shared partials.
/// </summary>
public class ConfigInputModel
{
    /// <summary>The display whose config is being edited.</summary>
    public required Display Display { get; init; }

    /// <summary>The config key name — used as both the HTML id/name and the lookup key.</summary>
    public required string Key { get; init; }

    /// <summary>Human-readable label shown next to the input.</summary>
    public required string Label { get; init; }

    /// <summary>Optional helper text rendered below the input.</summary>
    public string? HelpText { get; init; }

    /// <summary>
    /// Bootstrap column class applied to the wrapping div, e.g. <c>"col-md-4"</c>.
    /// Defaults to <c>"col-md-12"</c>.
    /// </summary>
    public string ColClass { get; init; } = "col-md-12";

    /// <summary>
    /// Extra CSS margin/spacing class applied to the wrapping div, e.g. <c>"mt-3"</c>.
    /// Defaults to empty string.
    /// </summary>
    public string MarginClass { get; init; } = string.Empty;

    /// <summary>
    /// For text/number inputs: the HTML input type (e.g. <c>"text"</c>, <c>"number"</c>).
    /// Defaults to <c>"text"</c>.
    /// </summary>
    public string InputType { get; init; } = "text";

    /// <summary>
    /// For number inputs: the step attribute value (e.g. <c>"0.01"</c>).
    /// Only applied when <see cref="InputType"/> is <c>"number"</c>.
    /// </summary>
    public string? Step { get; init; }

    /// <summary>
    /// When <c>true</c> the input is rendered as disabled (read-only display).
    /// </summary>
    public bool Disabled { get; init; }
}
