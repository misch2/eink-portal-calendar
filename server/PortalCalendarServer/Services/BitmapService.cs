namespace PortalCalendarServer.Services;

public class BitmapService(
    IDisplayService displayService,
    PageGeneratorService pageGeneratorService,
    ILogger<BitmapService> logger)
{
    private readonly IDisplayService _displayService = displayService;
    private readonly PageGeneratorService _pageGeneratorService = pageGeneratorService;
    private readonly ILogger<BitmapService> _logger = logger;

    /// <summary>
    /// Builds a <see cref="BitmapResult"/> for the given display using the supplied rendering options.
    /// Returns <c>null</c> when the display, its rendered bitmap, or its display-type information cannot be found;
    /// the <paramref name="errorMessage"/> out-parameter will contain a human-readable reason in that case.
    /// </summary>
    public virtual BitmapResult? GetBitmap(
        int displayId,
        out string? errorMessage,
        int? rotate = null,
        string? flip = null,
        double? gamma = null,
        int? colors = null,
        string? colormap_name = null,
        string format = "png",
        bool preview_colors = false)
    {
        errorMessage = null;

        var display = _displayService.GetDisplayById(displayId);
        if (display == null)
        {
            errorMessage = "Display not found";
            return null;
        }
        if (display.RenderedAt == null)
        {
            errorMessage = "No rendered bitmap available for this display yet";
            return null;
        }
        if (display.DisplayType == null)
        {
            errorMessage = "Display type information is missing for this display";
            return null;
        }

        rotate ??= display.Rotation;
        flip ??= "";
        gamma ??= display.Gamma;
        colors ??= display.DisplayType?.NumColors;

        var color_palette = display.ColorPalette(preview_colors);
        if (color_palette.Count == 0)
        {
            colormap_name = "webmap"; // FIXME is it ideal to use this as a fallback when no colors are defined? maybe we should have a "default" colormap that is just black and white or something? Or throw an error instead?
            _logger.LogWarning("No colors defined for display {DisplayId}, using fallback colormap {ColormapName}", display.Id, colormap_name);
        }

        var bitmapOptions = new BitmapOptions
        {
            Rotate = rotate!.Value,
            Flip = flip,
            Gamma = gamma!.Value,
            NumColors = colors!.Value,
            ColormapName = colormap_name,
            ColormapColors = color_palette,
            Format = format,
            DisplayType = display.DisplayType!,
            DitheringType = display.DitheringTypeCode
        };

        return _pageGeneratorService.ConvertStoredBitmap(display, bitmapOptions);
    }
}
