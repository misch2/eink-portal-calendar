namespace PortalCalendarServer.Models.POCOs;

/// <summary>
/// Represents a request to regenerate an image for a display
/// </summary>
public record ImageRegenerationRequest
{
    public int DisplayId { get; init; }
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a unique key for deduplication (one task per display)
    /// </summary>
    public string GetKey() => $"regenerate_{DisplayId}";
}
