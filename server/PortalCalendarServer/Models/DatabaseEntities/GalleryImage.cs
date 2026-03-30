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
        public long? FileSize { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }

        public List<GalleryImageLink> GalleryLinks { get; set; } = [];
        public List<Gallery> Galleries { get; set; } = [];
    }
}