namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class ColorVariant
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string DisplayTypeCode { get; set; }
        public int SortOrder { get; set; }
        public DisplayType DisplayType { get; set; } = null!;

        public ICollection<EpdColor> EpdColors { get; set; } = null!;
        public ICollection<Display> Displays { get; set; } = null!;
    }
}
