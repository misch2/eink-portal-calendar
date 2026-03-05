using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Modules;
using PortalCalendarServer.Services.PageGeneratorComponents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System.Security.Cryptography;
using System.Text;

namespace PortalCalendarServer.Services;

public class PageGeneratorService
{
    private readonly ILogger<PageGeneratorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IDisplayService _displayService;
    private readonly IWeb2PngService _web2PngService;
    private readonly InternalTokenService _internalTokenService;
    private readonly LinkGenerator _linkGenerator;
    private readonly ModuleRegistry _moduleRegistry;
    private readonly IServiceProvider _services;

    public PageGeneratorService(
        ILogger<PageGeneratorService> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IDisplayService displayService,
        IWeb2PngService web2PngService,
        InternalTokenService internalTokenService,
        LinkGenerator linkGenerator,
        ModuleRegistry moduleRegistry,
        IServiceProvider services)
    {
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
        _displayService = displayService;
        _web2PngService = web2PngService;
        _internalTokenService = internalTokenService;
        _linkGenerator = linkGenerator;
        _moduleRegistry = moduleRegistry;
        _services = services;
    }

    public PageViewModel PageViewModelForDate(Display display, DateTime date, bool previewColors = false)
    {
        var viewModel = new PageViewModel
        {
            Display = display,
            Date = date,
            CssColors = display.CssColorMap(previewColors)
        };

        foreach (var module in _moduleRegistry.All)
        {
            viewModel.RegisterComponent(module.ModuleId, () => module.CreatePageGeneratorComponent(_services, display, date));
        }

        return viewModel;
    }

    public string DisplayImageName(Display display)
    {
        var imagePath = _configuration["Paths:GeneratedImages"]
            ?? throw new InvalidOperationException("GeneratedImages path is not configured");

        var ret = Path.Combine(imagePath, $"current_calendar_{display.Id}.png");

        return ret;
    }

    private string DisplayIntermediateImageName(Display display)
    {
        var imagePath = _configuration["Paths:GeneratedImages"]
            ?? throw new InvalidOperationException("GeneratedImages path is not configured");

        var ret = Path.Combine(imagePath, $"current_calendar_{display.Id}-intermediate.png");

        return ret;
    }

    public async Task GenerateImageFromWebAsync(Display display)
    {
        var baseUrl = _configuration["URLs:BaseURL"];
        if (baseUrl == null)
        {
            throw new InvalidOperationException("BaseURL is not configured");
        }

        var url = _linkGenerator.GetUriByName(
                Controllers.Constants.CalendarHtmlDefaultDate,
                new { displayNumber = display.Id, preview_colors = false },
                scheme: new Uri(baseUrl).Scheme,
                host: new HostString(new Uri(baseUrl).Authority))
            ?? throw new InvalidOperationException("Could not generate URL for CalendarHtmlDefaultDate");

        var outputPath = DisplayImageName(display);
        _logger.LogInformation("Generating calendar image from URL {Url} to {OutputPath}", url, outputPath);

        var headers = new Dictionary<string, string>
        {
            [InternalTokenAuthenticationHandler.HeaderName] = _internalTokenService.Token
        };

        try
        {
            await _web2PngService.ConvertUrlAsync(
                url,
                display.VirtualWidth(),
                display.VirtualHeight(),
                outputPath,
                extraHeaders: headers);

            _logger.LogInformation("Image generation completed for display {DisplayId}", display.Id);
            _displayService.UpdateRenderInfo(display, DateTime.UtcNow, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image for display {DisplayId}", display.Id);
            var fullException = ex.Message;
            if (ex.InnerException != null)
            {
                fullException += " | Inner Exception: " + ex.InnerException.Message;
            }
            _displayService.UpdateRenderInfo(display, DateTime.UtcNow, fullException);

            // Try to generate an error page bitmap as fallback
            try
            {
                var errorUrl = _linkGenerator.GetUriByName(
                        Controllers.Constants.CalendarHtmlDefaultDate,
                        new { displayNumber = display.Id, preview_colors = false, force_error = fullException },
                        scheme: new Uri(baseUrl).Scheme,
                        host: new HostString(new Uri(baseUrl).Authority))
                    ?? throw new InvalidOperationException("Could not generate error URL for CalendarHtmlDefaultDate");

                _logger.LogInformation("Attempting to generate error page bitmap from {ErrorUrl}", errorUrl);

                await _web2PngService.ConvertUrlAsync(
                    errorUrl,
                    display.VirtualWidth(),
                    display.VirtualHeight(),
                    outputPath,
                    extraHeaders: headers);

                _logger.LogInformation("Error page bitmap generated successfully for display {DisplayId}", display.Id);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Failed to generate error page bitmap for display {DisplayId}, removing stored PNG", display.Id);
                // Remove the PNG so the API returns 500 instead of serving a stale image
                try
                {
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                }
                catch (Exception deleteEx)
                {
                    _logger.LogError(deleteEx, "Failed to delete stale PNG at {OutputPath}", outputPath);
                }
            }
        }
    }

    /// <summary>
    /// Generate bitmap image from source PNG
    /// </summary>
    public BitmapResult ConvertStoredBitmap(Display display, BitmapOptions options)
    {
        _logger.LogDebug("Converting pre-generated bitmap");

        var imagePath = DisplayImageName(display);
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        using var img = Image.Load<Rgba32>(imagePath);

        // If the generated image is larger (probably due to invalid CSS), crop it
        if (img.Height > display.VirtualHeight())
        {
            img.Mutate(x => x.Crop(new Rectangle(0, 0, display.VirtualWidth(), display.VirtualHeight())));
        }

        // Rotate
        if (options.Rotate != 0)
        {
            var angle = options.Rotate switch
            {
                1 => RotateMode.Rotate90,
                2 => RotateMode.Rotate180,
                3 => RotateMode.Rotate270,
                _ => throw new ArgumentException($"Unknown 'rotate' value: {options.Rotate}")
            };
            img.Mutate(x => x.Rotate(angle));
        }

        // Flip
        if (!string.IsNullOrEmpty(options.Flip))
        {
            switch (options.Flip.ToLower())
            {
                case "x":
                    img.Mutate(x => x.Flip(FlipMode.Horizontal));
                    break;
                case "y":
                    img.Mutate(x => x.Flip(FlipMode.Vertical));
                    break;
                case "xy":
                    img.Mutate(x => x.Flip(FlipMode.Horizontal).Flip(FlipMode.Vertical));
                    break;
                default:
                    throw new ArgumentException($"Unknown 'flip' value: {options.Flip}");
            }
        }

        // Apply gamma correction using lookup table
        if (options.Gamma != 1.0)
        {
            var lookupTable = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                lookupTable[i] = (byte)Math.Clamp(Math.Pow(i / 255.0, options.Gamma) * 255.0 + 0.5, 0, 255);
            }

            img.Mutate(ctx =>
            {
                ctx.ProcessPixelRowsAsVector4((span) =>
                {
                    for (int i = 0; i < span.Length; i++)
                    {
                        ref var pixel = ref span[i];
                        pixel.X = lookupTable[(byte)(pixel.X * 255)] / 255f;
                        pixel.Y = lookupTable[(byte)(pixel.Y * 255)] / 255f;
                        pixel.Z = lookupTable[(byte)(pixel.Z * 255)] / 255f;
                    }
                });
            });
        }

        if (options.NumColors < 256)
        {
            // Convert to palette-based image with specified number of colors
            var dithering = options.DitheringType?.ToLower() switch
            {
                "fs" => KnownDitherings.FloydSteinberg,
                "at" => KnownDitherings.Atkinson,
                "jjn" => KnownDitherings.JarvisJudiceNinke,
                "st" => KnownDitherings.Stucki,
                _ => null
            };

            var quantizerOptions = new QuantizerOptions
            {
                Dither = dithering,
                MaxColors = options.NumColors,
            };
            PaletteQuantizer quantizer;

            if (string.IsNullOrEmpty(options.ColormapName))
            {
                var palette = new List<Color>();
                palette.AddRange(options.ColormapColors);
                quantizer = new PaletteQuantizer(palette.ToArray(), quantizerOptions);
            }
            else if (options.ColormapName == "webmap")
            {
                quantizer = new WebSafePaletteQuantizer(quantizerOptions);
            }
            else
            {
                throw new ArgumentException($"Unknown colormap name: {options.ColormapName}");
            }

            img.Mutate(x => x.Quantize(quantizer));
        }

        // Save intermediate bitmap for debugging purposes
        var intermediatePath = DisplayIntermediateImageName(display);
        img.SaveAsPng(intermediatePath);

        // Generate output based on format
        if (options.Format == "png")
        {
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return new BitmapResult { Data = ms.ToArray(), ContentType = "image/png" };
        }
        else if (options.Format == "png_gray")
        {
            img.Mutate(x => x.Grayscale());
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return new BitmapResult { Data = ms.ToArray(), ContentType = "image/png" };
        }
        else if (options.Format == "epaper_native")
        {
            // FIXME use a reasonable default! Or, ideally, in the setup and have this as non-nullable and required in BitmapOptions
            var colorVariant = display.ColorVariant
                ?? throw new InvalidOperationException("Display has no ColorVariant assigned");
            var bitmap = _convertToEpaperFormat(img, colorVariant);
            var checksum = ComputeSHA1(bitmap);

            // Output format: "MM\n" + checksum + "\n" + bitmap data
            var output = Encoding.ASCII.GetBytes("MM\n")
                .Concat(Encoding.ASCII.GetBytes(checksum + "\n"))
                .Concat(bitmap)
                .ToArray();

            return new BitmapResult
            {
                Data = output,
                ContentType = "application/octet-stream",
                Headers = new Dictionary<string, string>
                {
                    ["Content-Transfer-Encoding"] = "binary"
                }
            };
        }
        else
        {
            throw new ArgumentException($"Unknown format requested: {options.Format}");
        }
    }

    /// <summary>
    /// Convert image to e-paper native format (BW, 4G, 3C, etc.)
    /// </summary>
    private byte[] _convertToEpaperFormat(Image<Rgba32> img, ColorVariant colorVariant)
    {
        using var ms = new MemoryStream();

        var displayType = colorVariant.DisplayType
            ?? throw new InvalidOperationException("ColorVariant has no associated DisplayType");

        // Process each row of pixels
        img.ProcessPixelRows(accessor =>
        {
            if (displayType.Code == "BW") // FIXME constant
            {
                // 1-bit black and white, 8 pixels per byte
                for (int y = 0; y < accessor.Height; y++)
                {
                    var rowSpan = accessor.GetRowSpan(y);
                    byte currentByte = 0;
                    int bitCount = 0;

                    foreach (var pixel in rowSpan)
                    {
                        var gray = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                        var bit = gray > 128 ? 1 : 0;
                        currentByte = (byte)((currentByte << 1) | bit);
                        bitCount++;

                        if (bitCount == 8)
                        {
                            ms.WriteByte(currentByte);
                            currentByte = 0;
                            bitCount = 0;
                        }
                    }
                }
            }
            else if (displayType.Code == "3C" || displayType.Code == "4C") // FIXME constant
            {
                // 3-color (black, white, red/yellow) - dual buffers per row
                // 4-color (black, white, red, yellow) - dtto but with a different color bit interpretation
                for (int y = 0; y < accessor.Height; y++)
                {
                    var rowSpan = accessor.GetRowSpan(y);
                    var bufferBW = new List<byte>();
                    var bufferColor = new List<byte>();
                    byte byteBW = 0;
                    byte byteColor = 0;
                    int bitCount = 0;

                    foreach (var pixel in rowSpan)
                    {
                        // The image is already quantized to the exact palette, so classify
                        // each pixel by its RGB values into one of the known e-paper colors.
                        var detectedColor = ClassifyPixelColor(pixel);

                        // Dual-buffer encoding:
                        //   mono buffer | color buffer | result
                        //   -----------   ------------   ----
                        //       0              1         black
                        //       1              1         white
                        //       0              0         red   (or the single accent color for 3C)
                        //       1              0         yellow (same as red/accent for 3C)
                        var (bwBit, colorBit) = detectedColor switch
                        {
                            DetectedEpdColor.Black => ((byte)0, (byte)1),
                            DetectedEpdColor.White => ((byte)1, (byte)1),
                            DetectedEpdColor.Yellow => ((byte)0, (byte)0),
                            DetectedEpdColor.Red => ((byte)1, (byte)0),
                            _ => throw new InvalidOperationException(
                                $"Unexpected pixel color ({pixel.R}, {pixel.G}, {pixel.B}) " +
                                $"could not be classified for display type {displayType.Code}")
                        };

                        byteBW = (byte)((byteBW << 1) | bwBit);
                        byteColor = (byte)((byteColor << 1) | colorBit);
                        bitCount++;

                        if (bitCount == 8)
                        {
                            bufferBW.Add(byteBW);
                            bufferColor.Add(byteColor);
                            byteBW = 0;
                            byteColor = 0;
                            bitCount = 0;
                        }
                    }

                    // Write BW buffer for this row
                    foreach (var b in bufferBW)
                    {
                        ms.WriteByte(b);
                    }
                    // Write color buffer for this row
                    foreach (var b in bufferColor)
                    {
                        ms.WriteByte(b);
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Unknown display type: {displayType}");
            }
        });

        return ms.ToArray();
    }

    /// <summary>
    /// Possible e-paper colors detected from a quantized pixel.
    /// </summary>
    private enum DetectedEpdColor { Black, White, Red, Yellow }

    /// <summary>
    /// Classify a quantized pixel into the closest e-paper color.
    /// The image is expected to already be quantized to the exact palette
    /// (black=#000, white=#FFF, red=#F00, yellow=#FF0), so simple
    /// channel thresholds are sufficient to handle minor rounding.
    /// </summary>
    private static DetectedEpdColor ClassifyPixelColor(Rgba32 pixel)
    {
        bool rHigh = pixel.R > 128;
        bool gHigh = pixel.G > 128;
        bool bHigh = pixel.B > 128;

        return (rHigh, gHigh, bHigh) switch
        {
            (false, false, false) => DetectedEpdColor.Black,   // #000000
            (true, true, true) => DetectedEpdColor.White,   // #FFFFFF
            (true, false, false) => DetectedEpdColor.Red,     // #FF0000
            (true, true, false) => DetectedEpdColor.Yellow,  // #FFFF00
            // Fallback: classify by luminance as black or white
            _ => (pixel.R + pixel.G + pixel.B) / 3 > 128
                ? DetectedEpdColor.White
                : DetectedEpdColor.Black,
        };
    }

    private static string ComputeSHA1(byte[] data)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

// Supporting classes
public class PageViewModel
{
    public required Display Display { get; set; }
    public required DateTime Date { get; set; }
    public required Dictionary<string, string> CssColors { get; set; }

    private readonly Dictionary<string, object?> _components = new();

    internal void RegisterComponent(string moduleId, Func<object?> componentFactory)
    {
        _components[moduleId] = componentFactory();
    }

    /// <summary>
    /// Retrieve a module's page-generator component by module ID.
    /// Returns <c>null</c> when the module is not registered or provides no component.
    /// </summary>
    public T? GetComponent<T>(string moduleId) where T : class
    {
        return _components.TryGetValue(moduleId, out var obj) ? obj as T : null;
    }

    // ── Backward-compatible shim properties for existing Razor views ─────────

    public PortalIconsComponent? PortalIcons => GetComponent<PortalIconsComponent>("portalicons");
    public CalendarComponent? Calendar => GetComponent<CalendarComponent>("calendar");
    public WeightComponent? Weight => GetComponent<WeightComponent>("googlefit");
    public XkcdComponent? Xkcd => GetComponent<XkcdComponent>("xkcd");
    public PublicHolidayComponent? PublicHoliday => GetComponent<PublicHolidayComponent>("publicholiday");
    public NameDayComponent? NameDay => GetComponent<NameDayComponent>("nameday");
    public WeatherComponent? Weather => GetComponent<WeatherComponent>("metnoweather");
}
public class BitmapOptions
{
    public int Rotate { get; set; }
    public string Flip { get; set; } = string.Empty;
    public double Gamma { get; set; } = 1.0;
    public int NumColors { get; set; } = 256;
    /// <summary>
    /// Either "none" or "webmap". Originally used for the Perl Imager module.
    /// </summary>
    public string? ColormapName { get; set; }
    public List<Color> ColormapColors { get; set; } = new();
    public string Format { get; set; } = "png";
    public required DisplayType DisplayType { get; set; }
    public string? DitheringType { get; set; } = null;
}

public class BitmapResult
{
    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = "image/png";
    public Dictionary<string, string>? Headers { get; set; }
}
