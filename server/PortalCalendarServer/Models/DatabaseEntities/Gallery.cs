namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class Gallery
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? OwnerId { get; set; }
        public AppUser? Owner { get; set; }
        public bool HideFromOtherUsers { get; set; }

        public List<GalleryImageLink> ImageLinks { get; set; } = [];
        public List<GalleryImage> Images { get; set; } = [];
    }
}