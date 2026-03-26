namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class GalleryImageLink
    {
        public int GalleryId { get; set; }
        public int GalleryImageId { get; set; }
        public bool IsHidden { get; set; }

        public Gallery Gallery { get; set; } = null!;
        public GalleryImage GalleryImage { get; set; } = null!;
    }
}
