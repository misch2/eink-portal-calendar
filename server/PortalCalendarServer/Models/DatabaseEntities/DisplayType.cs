namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class DisplayType
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required int NumColors { get; set; }
        public int SortOrder { get; set; }

        public ICollection<Display> Displays { get; set; } = null!;
        public ICollection<ColorVariant> ColorVariants { get; set; } = null!;
    }
}
