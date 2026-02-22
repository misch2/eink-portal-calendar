namespace PortalCalendarServer.Models.DatabaseEntities;

public partial class Display
{
    public int Id { get; set; }

    public string Mac { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Width { get; set; }

    public int Height { get; set; }

    public int Rotation { get; set; }

    public string? DisplayTypeCode { get; set; }
    public virtual DisplayType? DisplayType { get; set; }

    public string? ColorVariantCode { get; set; }
    public virtual ColorVariant? ColorVariant { get; set; }

    public double? Gamma { get; set; }

    public int BorderTop { get; set; }

    public int BorderRight { get; set; }

    public int BorderBottom { get; set; }

    public int BorderLeft { get; set; }

    public string? Firmware { get; set; }

    public int? ThemeId { get; set; }
    public virtual Theme? Theme { get; set; }

    public string? DitheringTypeCode { get; set; }
    public virtual DitheringType? DitheringType { get; set; }

    public DateTime? RenderedAt { get; set; }
    public string? RenderErrors { get; set; }

    public virtual ICollection<Config> Configs { get; set; } = new List<Config>();
}
