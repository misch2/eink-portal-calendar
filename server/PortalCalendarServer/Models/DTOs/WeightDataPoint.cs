namespace PortalCalendarServer.Models.DTOs;

/// <summary>
/// Represents a weight data point for a specific date.
/// </summary>
public class WeightDataPoint
{
    public DateTime Date { get; set; }
    public decimal Weight { get; set; }
}
