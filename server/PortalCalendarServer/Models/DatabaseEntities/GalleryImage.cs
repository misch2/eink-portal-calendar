namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class GalleryImage
    {
        public int Id { get; set; }
        public required string PrimaryFolder { get; set; }
        public required string FileName { get; set; }
        public string? Description { get; set; }
        public required string ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsHidden { get; set; }

        public List<Gallery> Galleries { get; set; } = [];
    }
}