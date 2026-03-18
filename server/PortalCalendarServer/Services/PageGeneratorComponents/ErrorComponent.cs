namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component that carries error information for the error theme view.
/// </summary>
public class ErrorComponent
{
    public required string Message { get; set; }
    public string? Details { get; set; }
    public bool ShowDetails { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
