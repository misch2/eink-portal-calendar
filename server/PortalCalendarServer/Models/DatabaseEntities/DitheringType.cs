namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class DitheringType
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public int SortOrder { get; set; } = 0;
        public ICollection<Display> Displays { get; set; } = null!;
    }
}
