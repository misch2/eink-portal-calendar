namespace PortalCalendarServer.Models.POCOs;

/// <summary>
/// View model passed to the <c>_ConfigSelect</c> shared partial.
/// </summary>
public class ConfigSelectModel : ConfigInputModel
{
    /// <summary>
    /// The available options for the select element.
    /// Key = option value attribute, Value = display text.
    /// </summary>
    public required IReadOnlyList<SelectOption> Options { get; init; }

    /// <summary>
    /// When <c>true</c>, an empty option is prepended to the list
    /// so the user can explicitly clear the selection.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool AllowEmpty { get; init; } = false;
}

public class SelectOption
{
    public required string Value { get; init; }
    public required string Text { get; init; }
}
