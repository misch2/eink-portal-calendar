namespace PortalCalendarServer.Models.DatabaseEntities;

public partial class Config
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public int DisplayId { get; set; }

    public Display Display { get; set; } = null!;
}
