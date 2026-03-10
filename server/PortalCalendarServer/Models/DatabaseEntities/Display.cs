using PortalCalendarServer.Models.POCOs;

namespace PortalCalendarServer.Models.DatabaseEntities;

public partial class Display
{
    public int Id { get; set; }

    public string? Mac { get; set; } = null;

    public string? Name { get; set; } = null;

    public int Width { get; set; } = 0;

    public int Height { get; set; } = 0;

    public DisplayRotation Rotation { get; set; }

    public required string DisplayTypeCode { get; set; }

    public DisplayType DisplayType { get; set; } = null!;

    public required string ColorVariantCode { get; set; }

    public ColorVariant ColorVariant { get; set; } = null!;

    public double? Gamma { get; set; }

    public int BorderTop { get; set; }

    public int BorderRight { get; set; }

    public int BorderBottom { get; set; }

    public int BorderLeft { get; set; }

    public string? Firmware { get; set; }

    public int? ThemeId { get; set; }
    public Theme? Theme { get; set; }

    public string? DitheringTypeCode { get; set; }
    public DitheringType? DitheringType { get; set; }

    public DateTime? RenderedAt { get; set; }
    public string? RenderErrors { get; set; }

    public ICollection<Config> Configs { get; set; } = new List<Config>();
}
