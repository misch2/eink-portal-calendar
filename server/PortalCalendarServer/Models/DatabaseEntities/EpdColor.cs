namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class EpdColor
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string HexValue { get; set; } = null!;
        public string EpdPreviewHexValue { get; set; } = null!;

        //public virtual ICollection<ColorPaletteLink> ColorPaletteLinks { get; set; } = new List<ColorPaletteLink>();
    }
}
