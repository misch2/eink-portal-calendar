namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class DisplayType
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required int NumColors { get; set; }

        public virtual ICollection<Display> Displays { get; set; } = new List<Display>();
        public virtual ICollection<ColorVariant> ColorVariants { get; set; } = new List<ColorVariant>();
    }
}
