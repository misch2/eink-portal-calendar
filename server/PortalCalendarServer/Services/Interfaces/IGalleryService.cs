using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Services
{
    public interface IGalleryService
    {
        Task<List<Gallery>> GetAllGalleriesAsync();
        Task<Gallery?> GetGalleryByIdAsync(int id);
        Task<Gallery> CreateGalleryAsync(string name);
        Task DeleteGalleryAsync(int id);

        Task<GalleryImage> AddImageAsync(int galleryId, IFormFile file, string? description);
        Task DeleteImageAsync(int imageId);
        Task UpdateImageDescriptionAsync(int imageId, string? description);
        string GetImageFilePath(GalleryImage image);
    }
}
