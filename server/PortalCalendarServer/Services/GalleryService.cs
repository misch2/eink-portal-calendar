using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using SixLabors.ImageSharp;

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

    // For listing galleries without loading all images (e.g. for gallery overview)
    public async Task<List<Gallery>> GetFastGalleryListAsync()
    {
        return await _context.Galleries
            .OrderBy(g => g.Id)
            .ToListAsync();
    }

    public async Task<Gallery?> GetGalleryByIdAsync(int id)
    {
        return await _context.Galleries
            .Include(g => g.Images)
            .Include(g => g.ImageLinks)
            .AsSingleQuery()
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

    public async Task<Gallery> CopyGalleryAsync(int sourceGalleryId, string newName)
    {
        var source = await _context.Galleries
            .Include(g => g.Images)
            .FirstOrDefaultAsync(g => g.Id == sourceGalleryId);
        if (source == null) throw new ArgumentException($"Gallery {sourceGalleryId} not found");

        var copy = new Gallery
        {
            Name = newName,
            CreatedAt = DateTime.UtcNow
        };

        // Link the same images (many-to-many, no file duplication)
        foreach (var image in source.Images)
        {
            copy.Images.Add(image);
        }

        _context.Galleries.Add(copy);
        await _context.SaveChangesAsync();

        return copy;
    }

    public async Task RenameGalleryAsync(int id, string newName)
    {
        var gallery = await _context.Galleries.FindAsync(id);
        if (gallery == null) throw new ArgumentException($"Gallery {id} not found");

        gallery.Name = newName;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteGalleryAsync(int id)
    {
        var gallery = await _context.Galleries
            .Include(g => g.Images)
                .ThenInclude(i => i.Galleries)
            .AsSingleQuery()
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

        image.FileSize = new FileInfo(filePath).Length;
        PopulateImageDimensions(image, filePath);

        image.Galleries.Add(gallery);
        _context.GalleryImages.Add(image);
        await _context.SaveChangesAsync();

        return image;
    }

    public async Task DeleteImageAsync(int galleryId, int imageId)
    {
        var image = await _context.GalleryImages
            .Include(i => i.Galleries)
            .FirstOrDefaultAsync(i => i.Id == imageId);
        if (image == null) return;

        var gallery = image.Galleries.FirstOrDefault(g => g.Id == galleryId);
        if (gallery != null)
        {
            image.Galleries.Remove(gallery);
        }

        // Only delete the file and entity if the image is no longer in any gallery
        if (image.Galleries.Count == 0)
        {
            var filePath = Path.Combine(GetImageDirectory(image.PrimaryFolder), image.FileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _context.GalleryImages.Remove(image);
        }

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

        image.FileSize = new FileInfo(newPath).Length;
        PopulateImageDimensions(image, newPath);

        await _context.SaveChangesAsync();
    }

    public async Task SetImageVisibilityAsync(int galleryId, int imageId, bool isHidden)
    {
        var gallery = await _context.Galleries
            .Include(g => g.ImageLinks)
            .FirstOrDefaultAsync(g => g.Id == galleryId);
        if (gallery == null) return;

        var link = gallery.ImageLinks.FirstOrDefault(l => l.GalleryImageId == imageId);
        if (link == null) return;

        link.IsHidden = isHidden;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SetImageRotationAsync(int galleryId, int imageId, int rotation)
    {
        // Normalize to a valid multiple of 90 in [0, 360)
        rotation = ((rotation % 360) + 360) % 360;
        rotation = (rotation / 90) * 90;

        var gallery = await _context.Galleries
            .Include(g => g.ImageLinks)
            .FirstOrDefaultAsync(g => g.Id == galleryId);
        if (gallery == null) return false;

        var link = gallery.ImageLinks.FirstOrDefault(l => l.GalleryImageId == imageId);
        if (link == null) return false;

        link.Rotation = rotation;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task CopyImageToGalleryAsync(int imageId, int targetGalleryId)
    {
        var image = await _context.GalleryImages
            .Include(i => i.Galleries)
            .FirstOrDefaultAsync(i => i.Id == imageId);
        if (image == null) return;

        var targetGallery = await _context.Galleries.FindAsync(targetGalleryId);
        if (targetGallery == null) return;

        if (image.Galleries.Any(g => g.Id == targetGalleryId)) return;

        image.Galleries.Add(targetGallery);
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

    private static void PopulateImageDimensions(GalleryImage image, string filePath)
    {
        try
        {
            var info = Image.Identify(filePath);
            if (info != null)
            {
                image.Width = info.Width;
                image.Height = info.Height;
            }
        }
        catch
        {
            // Not a recognized image format — leave dimensions null
        }
    }
}
