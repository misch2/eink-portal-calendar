namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class ColorVariant
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string DisplayTypeCode { get; set; }
        public virtual DisplayType DisplayType { get; set; } = null!;
        public virtual ICollection<ColorPaletteLink> ColorPaletteLinks { get; set; } = new List<ColorPaletteLink>();
        //public virtual ICollection<Display> Displays { get; set; } = new List<Display>();
    }
}
