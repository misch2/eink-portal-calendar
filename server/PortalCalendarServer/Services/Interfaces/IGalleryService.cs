using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Services
{
    public interface IGalleryService
    {
        Task<List<Gallery>> GetAllGalleriesAsync();
        Task<Gallery?> GetGalleryByIdAsync(int id);
        Task<Gallery> CreateGalleryAsync(string name);
        Task<Gallery> CopyGalleryAsync(int sourceGalleryId, string newName);
        Task RenameGalleryAsync(int id, string newName);
        Task DeleteGalleryAsync(int id);

        Task<GalleryImage> AddImageAsync(int galleryId, IFormFile file, string? description);
        Task DeleteImageAsync(int galleryId, int imageId);
        Task UpdateImageDescriptionAsync(int imageId, string? description);
        Task ReplaceImageFileAsync(int imageId, IFormFile file);
        string GetImageFilePath(GalleryImage image);
    }
}
