using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalCalendarServer.Services;

namespace PortalCalendarServer.Controllers;

[Controller]
[Authorize]
public class GalleriesController(GalleryService galleryService) : Controller
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

        ViewData["NavLink"] = "galleries";
        ViewData["Title"] = gallery.Name;

        return View("~/Views/Galleries/Detail.cshtml", gallery);
    }

    // POST /galleries/{id}/upload
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
        await galleryService.DeleteImageAsync(imageId);
        TempData["Message"] = "Image deleted.";
        return RedirectToAction(nameof(Detail), new { id = galleryId });
    }

    // GET /galleries/{galleryId}/images/{imageId}
    [HttpGet("/galleries/{galleryId:int}/images/{imageId:int}")]
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
