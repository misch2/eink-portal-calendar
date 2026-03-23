using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Services;

public class GalleryService(CalendarContext context, IConfiguration configuration) : IGalleryService
{
    private readonly CalendarContext _context = context;
    private readonly string _galleryImagesPath = configuration["Paths:GalleryImages"]!;

    public async Task<List<Gallery>> GetAllGalleriesAsync()
    {
        return await _context.Galleries
            .Include(g => g.Images)
            .OrderBy(g => g.Id)
            .ToListAsync();
    }

    public async Task<Gallery?> GetGalleryByIdAsync(int id)
    {
        return await _context.Galleries
            .Include(g => g.Images)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Gallery> CreateGalleryAsync(string name)
    {
        var gallery = new Gallery
        {
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        _context.Galleries.Add(gallery);
        await _context.SaveChangesAsync();

        // Create directory for gallery images
        Directory.CreateDirectory(GetGalleryDirectory(gallery.Id));

        return gallery;
    }

    public async Task DeleteGalleryAsync(int id)
    {
        var gallery = await _context.Galleries.FindAsync(id);
        if (gallery == null) return;

        // Delete gallery directory and all images on disk
        var galleryDir = GetGalleryDirectory(id);
        if (Directory.Exists(galleryDir))
        {
            Directory.Delete(galleryDir, recursive: true);
        }

        _context.Galleries.Remove(gallery);
        await _context.SaveChangesAsync();
    }

    public async Task<GalleryImage> AddImageAsync(int galleryId, IFormFile file, string? description)
    {
        var galleryDir = GetGalleryDirectory(galleryId);
        Directory.CreateDirectory(galleryDir);

        var image = new GalleryImage
        {
            GalleryId = galleryId,
            FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName),
            Description = description,
            ContentType = file.ContentType,
            UploadedAt = DateTime.UtcNow
        };

        var filePath = Path.Combine(galleryDir, image.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _context.GalleryImages.Add(image);
        await _context.SaveChangesAsync();

        return image;
    }

    public async Task DeleteImageAsync(int imageId)
    {
        var image = await _context.GalleryImages.FindAsync(imageId);
        if (image == null) return;

        var filePath = Path.Combine(GetGalleryDirectory(image.GalleryId), image.FileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        _context.GalleryImages.Remove(image);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateImageDescriptionAsync(int imageId, string? description)
    {
        var image = await _context.GalleryImages.FindAsync(imageId);
        if (image == null) return;

        image.Description = description;
        await _context.SaveChangesAsync();
    }

    public async Task ReplaceImageFileAsync(int imageId, IFormFile file)
    {
        var image = await _context.GalleryImages.FindAsync(imageId);
        if (image == null) return;

        var galleryDir = GetGalleryDirectory(image.GalleryId);

        // Delete old file
        var oldPath = Path.Combine(galleryDir, image.FileName);
        if (File.Exists(oldPath))
        {
            File.Delete(oldPath);
        }

        // Save new file with a new name
        image.FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        image.ContentType = file.ContentType;
        image.UploadedAt = DateTime.UtcNow;

        var newPath = Path.Combine(galleryDir, image.FileName);
        using (var stream = new FileStream(newPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        await _context.SaveChangesAsync();
    }

    public string GetImageFilePath(GalleryImage image)
    {
        return Path.Combine(GetGalleryDirectory(image.GalleryId), image.FileName);
    }

    private string GetGalleryDirectory(int galleryId)
    {
        return Path.Combine(_galleryImagesPath, galleryId.ToString());
    }
}
