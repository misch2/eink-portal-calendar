namespace PortalCalendarServer.Models.DatabaseEntities;

public class GalleryImage
{
    public int Id { get; set; }
    public int GalleryId { get; set; }
    public required string FileName { get; set; }
    public string? Description { get; set; }
    public required string ContentType { get; set; }
    public DateTime UploadedAt { get; set; }

    public Gallery Gallery { get; set; } = null!;
}
