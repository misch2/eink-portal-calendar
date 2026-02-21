namespace PortalCalendarServer.Models.POCOs;

/// <summary>
/// View model for the error theme displayed when calendar rendering fails.
/// </summary>
public class ErrorViewModel
{
    public required string Message { get; set; }
    public string? Details { get; set; }
    public bool ShowDetails { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string>? CssColors { get; set; }
}
