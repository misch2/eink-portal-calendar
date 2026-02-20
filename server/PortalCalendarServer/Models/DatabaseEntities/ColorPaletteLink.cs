namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class ColorPaletteLink
    {
        public int Id { get; set; }
        public required string ColorVariantCode { get; set; }
        public virtual ColorVariant ColorVariant { get; set; } = null!;
        public required string EpdColorCode { get; set; }
        public virtual EpdColor EpdColor { get; set; } = null!;
    }
}
