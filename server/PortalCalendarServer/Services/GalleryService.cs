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
        Directory.CreateDirectory(GetImageDirectory(gallery.Id.ToString()));

        return gallery;
    }

    public async Task DeleteGalleryAsync(int id)
    {
        var gallery = await _context.Galleries
            .Include(g => g.Images)
                .ThenInclude(i => i.Galleries)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (gallery == null) return;

        // Find images that only belong to this gallery
        var exclusiveImages = gallery.Images
            .Where(i => i.Galleries.Count == 1)
            .ToList();

        // Delete files of exclusive images from disk
        foreach (var image in exclusiveImages)
        {
            var filePath = Path.Combine(GetImageDirectory(image.PrimaryFolder), image.FileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            _context.GalleryImages.Remove(image);
        }

        _context.Galleries.Remove(gallery);
        await _context.SaveChangesAsync();
    }

    public async Task<GalleryImage> AddImageAsync(int galleryId, IFormFile file, string? description)
    {
        var gallery = await _context.Galleries.FindAsync(galleryId);
        if (gallery == null) throw new ArgumentException($"Gallery {galleryId} not found");

        var primaryFolder = galleryId.ToString();
        var imageDir = GetImageDirectory(primaryFolder);
        Directory.CreateDirectory(imageDir);

        var image = new GalleryImage
        {
            PrimaryFolder = primaryFolder,
            FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName),
            Description = description,
            ContentType = file.ContentType,
            UploadedAt = DateTime.UtcNow
        };

        var filePath = Path.Combine(imageDir, image.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        image.Galleries.Add(gallery);
        _context.GalleryImages.Add(image);
        await _context.SaveChangesAsync();

        return image;
    }

    public async Task DeleteImageAsync(int imageId)
    {
        var image = await _context.GalleryImages.FindAsync(imageId);
        if (image == null) return;

        var filePath = Path.Combine(GetImageDirectory(image.PrimaryFolder), image.FileName);
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

        var imageDir = GetImageDirectory(image.PrimaryFolder);

        // Delete old file
        var oldPath = Path.Combine(imageDir, image.FileName);
        if (File.Exists(oldPath))
        {
            File.Delete(oldPath);
        }

        // Save new file with a new name
        image.FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        image.ContentType = file.ContentType;
        image.UploadedAt = DateTime.UtcNow;

        var newPath = Path.Combine(imageDir, image.FileName);
        using (var stream = new FileStream(newPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        await _context.SaveChangesAsync();
    }

    public string GetImageFilePath(GalleryImage image)
    {
        return Path.Combine(GetImageDirectory(image.PrimaryFolder), image.FileName);
    }

    private string GetImageDirectory(string primaryFolder)
    {
        return Path.Combine(_galleryImagesPath, primaryFolder);
    }
}
