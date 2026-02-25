using Microsoft.AspNetCore.Mvc;
using PortalCalendarServer.Services;

namespace PortalCalendarServer.Controllers;

public static class BitmapControllerExtensions
{
    /// <summary>
    /// Writes a <see cref="BitmapResult"/> as the HTTP response, including any extra headers it carries.
    /// </summary>
    public static IActionResult ReturnBitmap(this ControllerBase controller, BitmapResult bitmap)
    {
        if (bitmap.Headers != null)
        {
            foreach (var header in bitmap.Headers)
            {
                controller.Response.Headers.Append(header.Key, header.Value);
            }
        }

        return controller.File(bitmap.Data, bitmap.ContentType);
    }
}
