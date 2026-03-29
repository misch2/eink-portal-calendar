using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalCalendarServer.Services;

namespace PortalCalendarServer.Controllers;

[Controller]
[Authorize]
public class GalleriesController(IGalleryService galleryService) : Controller
{
    // GET /galleries
    [HttpGet("/galleries")]
    public async Task<IActionResult> Index()
    {
        var galleries = await galleryService.GetAllGalleriesAsync();

        ViewData["NavLink"] = "galleries";
        ViewData["Title"] = "Galleries";

        return View("~/Views/Galleries/Index.cshtml", galleries);
    }

    // POST /galleries/create
    [HttpPost("/galleries/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Gallery name is required.";
            return RedirectToAction(nameof(Index));
        }

        await galleryService.CreateGalleryAsync(name);
        TempData["Message"] = $"Gallery '{name}' created.";
        return RedirectToAction(nameof(Index));
    }

    // POST /galleries/copy/{id}
    [HttpPost("/galleries/copy/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Copy(int id, [FromForm] string name)
    {
        var source = await galleryService.GetGalleryByIdAsync(id);
        if (source == null)
        {
            TempData["Error"] = "Source gallery not found.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Gallery name is required.";
            return RedirectToAction(nameof(Index));
        }

        var copy = await galleryService.CopyGalleryAsync(id, name);
        TempData["Message"] = $"Gallery '{source.Name}' copied as '{copy.Name}'.";
        return RedirectToAction(nameof(Index));
    }

    // POST /galleries/rename/{id}
    [HttpPost("/galleries/rename/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(int id, [FromForm] string name)
    {
        var gallery = await galleryService.GetGalleryByIdAsync(id);
        if (gallery == null)
        {
            TempData["Error"] = "Gallery not found.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Gallery name is required.";
            return RedirectToAction(nameof(Index));
        }

        var oldName = gallery.Name;
        await galleryService.RenameGalleryAsync(id, name);
        TempData["Message"] = $"Gallery '{oldName}' renamed to '{name}'.";
        return RedirectToAction(nameof(Index));
    }

    // POST /galleries/delete/{id}
    [HttpPost("/galleries/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var gallery = await galleryService.GetGalleryByIdAsync(id);
        if (gallery == null)
        {
            TempData["Error"] = "Gallery not found.";
            return RedirectToAction(nameof(Index));
        }

        await galleryService.DeleteGalleryAsync(id);
        TempData["Message"] = $"Gallery '{gallery.Name}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    // GET /galleries/{id}
    [HttpGet("/galleries/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var gallery = await galleryService.GetGalleryByIdAsync(id);
        if (gallery == null)
        {
            return NotFound();
        }

        ViewData["NavLink"] = "gallery";
        ViewData["GalleryId"] = gallery.Id;
        ViewData["Title"] = gallery.Name;
        ViewData["AllGalleries"] = await galleryService.GetAllGalleriesAsync();

        return View("~/Views/Galleries/Detail.cshtml", gallery);
    }

    // POST /galleries/{id}/upload
    [RequestSizeLimit(10 * 1024 * 1024)] // Limit uploads to 10 MB
    [HttpPost("/galleries/{id:int}/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int id, IFormFile file, [FromForm] string? description)
    {
        var gallery = await galleryService.GetGalleryByIdAsync(id);
        if (gallery == null)
        {
            return NotFound();
        }

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        await galleryService.AddImageAsync(id, file, description);
        TempData["Message"] = "Image uploaded.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    // POST /galleries/{galleryId}/images/delete/{imageId}
    [HttpPost("/galleries/{galleryId:int}/images/delete/{imageId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int galleryId, int imageId)
    {
        await galleryService.DeleteImageAsync(galleryId, imageId);
        TempData["Message"] = "Image deleted.";
        return RedirectToAction(nameof(Detail), new { id = galleryId });
    }

    // POST /galleries/{galleryId}/images/{imageId}/description
    [HttpPost("/galleries/{galleryId:int}/images/{imageId:int}/description")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateImage(int galleryId, int imageId, [FromForm] string? description, IFormFile? file)
    {
        await galleryService.UpdateImageDescriptionAsync(imageId, description);

        if (file != null && file.Length > 0)
        {
            await galleryService.ReplaceImageFileAsync(imageId, file);
        }

        TempData["Message"] = "Image updated.";
        return RedirectToAction(nameof(Detail), new { id = galleryId });
    }

    // POST /galleries/{galleryId}/images/{imageId}/toggle-visibility
    [HttpPost("/galleries/{galleryId:int}/images/{imageId:int}/toggle-visibility")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisibility(int galleryId, int imageId, [FromForm] bool isHidden)
    {
        await galleryService.SetImageVisibilityAsync(galleryId, imageId, isHidden);
        return RedirectToAction(nameof(Detail), new { id = galleryId });
    }

    // POST /galleries/{galleryId}/images/{imageId}/copy-to/{targetGalleryId}
    [HttpPost("/galleries/{galleryId:int}/images/{imageId:int}/copy-to/{targetGalleryId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyImageToGallery(int galleryId, int imageId, int targetGalleryId)
    {
        var targetGallery = await galleryService.GetGalleryByIdAsync(targetGalleryId);
        if (targetGallery == null)
        {
            TempData["Error"] = "Target gallery not found.";
            return RedirectToAction(nameof(Detail), new { id = galleryId });
        }

        await galleryService.CopyImageToGalleryAsync(imageId, targetGalleryId);
        TempData["Message"] = $"Image copied to '{targetGallery.Name}'.";
        return RedirectToAction(nameof(Detail), new { id = galleryId });
    }

    // GET /galleries/{galleryId}/images/{imageId}
    [HttpGet("/galleries/{galleryId:int}/images/{imageId:int}")]
    [Authorize("CookiesOrInternalToken")]
    public async Task<IActionResult> ServeImage(int galleryId, int imageId)
    {
        var gallery = await galleryService.GetGalleryByIdAsync(galleryId);
        if (gallery == null)
        {
            return NotFound();
        }

        var image = gallery.Images.FirstOrDefault(i => i.Id == imageId);
        if (image == null)
        {
            return NotFound();
        }

        var filePath = galleryService.GetImageFilePath(image);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        return PhysicalFile(filePath, image.ContentType);
    }
}
