namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class ColorPaletteLink
    {
        public int Id { get; set; }
        public required string ColorVariantCode { get; set; }
        public required string EpdColorCode { get; set; }

        public ColorVariant ColorVariant { get; set; } = null!;
        public EpdColor EpdColor { get; set; } = null!;
    }
}
