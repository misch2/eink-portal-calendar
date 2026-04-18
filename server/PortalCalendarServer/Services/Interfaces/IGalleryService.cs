using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Services
{
    public interface IGalleryService
    {
        Task<List<Gallery>> GetVisibleGalleriesAsync();
        Task<List<Gallery>> GetAllGalleriesUnfilteredAsync();
        Task<List<Gallery>> GetFastGalleryListAsync();
        Task<Gallery?> GetVisibleGalleryByIdAsync(int id);
        Task<Gallery?> GetGalleryByIdUnfilteredAsync(int id);
        Task<Gallery> CreateGalleryAsync(string name);
        Task<Gallery> CopyGalleryAsync(int sourceGalleryId, string newName);
        Task RenameGalleryAsync(int id, string newName);
        Task DeleteGalleryAsync(int id);

        Task<GalleryImage> AddImageAsync(int galleryId, IFormFile file, string? description);
        Task DeleteImageAsync(int galleryId, int imageId);
        Task UpdateImageDescriptionAsync(int imageId, string? description);
        Task<GalleryImage?> ReplaceImageFileAsync(int imageId, IFormFile file);
        Task SetImageVisibilityAsync(int galleryId, int imageId, bool isHidden);
        Task<bool> SetImageRotationAsync(int galleryId, int imageId, int rotation);
        Task CopyImageToGalleryAsync(int imageId, int targetGalleryId);
        string GetImageFilePath(GalleryImage image);
        Task SaveChangesAsync();
    }
}
