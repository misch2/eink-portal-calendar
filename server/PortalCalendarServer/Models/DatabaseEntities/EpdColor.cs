namespace PortalCalendarServer.Models.DatabaseEntities
{
    public partial class EpdColor
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string HexValue { get; set; } = null!;
        public string EpdPreviewHexValue { get; set; } = null!;

        public virtual ICollection<ColorVariant> ColorVariants { get; set; } = new List<ColorVariant>();
    }
}
