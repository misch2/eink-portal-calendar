using Microsoft.Playwright;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models;
using PortalCalendarServer.Services.PageGeneratorComponents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PortalCalendarServer.Services;

public class PageGeneratorService
{
    private readonly ILogger<PageGeneratorService> _logger;
    private readonly CalendarContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly DisplayService _displayService;
    private readonly Web2PngService _web2PngService;
    private readonly Display _display;
    private readonly int _minimalCacheExpiry;

    public PageGeneratorService(
        ILogger<PageGeneratorService> logger,
        CalendarContext context,
        IWebHostEnvironment environment,
        DisplayService displayService,
        Web2PngService web2PngService,
        Display display,
        int minimalCacheExpiry = 0)
    {
        _logger = logger;
        _context = context;
        _environment = environment;
        _displayService = displayService;
        _web2PngService = web2PngService;
        _display = display;
        _minimalCacheExpiry = minimalCacheExpiry;

        _displayService.UseDisplay(display);
    }

    public PageViewModel PageViewModelForDate(DateTime date, bool previewColors = false)
    {
        var seed = int.Parse(date.ToString("yyyyMMdd"));
        var random = new Random(seed);

        var viewModel = new PageViewModel
        {
            Display = _display,
            Date = date,
            CssColors = _display.CssColorMap(previewColors)
        };

        viewModel.InitializeComponents(
            portalIconsFactory: () => new PortalIconsComponent(_logger, _displayService, date),
            calendarFactory: () => new CalendarComponent(_logger, _displayService, date),
            weightFactory: () => new WeightComponent(_logger, date, random)
        );

        return viewModel;
    }


    public string DisplayImageName()
    {
        var imagePath = Path.Combine(_environment.ContentRootPath, "..");

        var subPath = $"generated_images/current_calendar_{_display.Id}.png";   // FIXME

        return Path.Combine(imagePath, subPath);
    }

    public void GenerateImageFromWeb()
    {
        var url = $"http://localhost:5252/calendar/{_display.Id}/html?preview_colors=true";  // FIXME FIXME hardcoded port!
        var outputPath = DisplayImageName();
        _logger.LogInformation("Generating calendar image from URL {Url} to {OutputPath}", url, outputPath);

        // FIXME await?
        // FIXME timeout?
        _web2PngService.ConvertUrlAsync(
            url,
            _display.VirtualWidth(),
            _display.VirtualHeight(),
            outputPath,
            2000).GetAwaiter().GetResult();

        return;
    }

    /// <summary>
    /// Generate bitmap image from source PNG
    /// </summary>
    public BitmapResult GenerateBitmap(BitmapOptions options)
    {
        _logger.LogDebug("Producing bitmap");

        var imagePath = DisplayImageName();
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        using var img = Image.Load<Rgba32>(imagePath);

        // If the generated image is larger (probably due to invalid CSS), crop it
        if (img.Height > _display.VirtualHeight())
        {
            img.Mutate(x => x.Crop(new Rectangle(0, 0, _display.VirtualWidth(), _display.VirtualHeight())));
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
            var quantizerOptions = new QuantizerOptions
            {
                Dither = null, // use closest color without dithering
                MaxColors = options.NumColors,
            };
            PaletteQuantizer quantizer;

            if (options.ColormapName == "none")
            {
                var palette = new List<Color>();
                palette.AddRange(options.ColormapColors.Select(c => new Color(Rgba32.ParseHex(c))));
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
            var bitmap = _convertToEpaperFormat(img, options.DisplayType);
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
    private byte[] _convertToEpaperFormat(Image<Rgba32> img, string displayType)
    {
        using var ms = new MemoryStream();

        // Process each row of pixels
        img.ProcessPixelRows(accessor =>
        {
            //if (displayType == "256G")
            //{
            //    // 8-bit grayscale, one byte per pixel
            //    for (int y = 0; y < accessor.Height; y++)
            //    {
            //        var rowSpan = accessor.GetRowSpan(y);
            //        foreach (var pixel in rowSpan)
            //        {
            //            var gray = (byte)((pixel.R + pixel.G + pixel.B) / 3);
            //            ms.WriteByte(gray);
            //        }
            //    }
            //}
            //else if (displayType == "4G")
            //{
            //    // 2-bit grayscale, 4 pixels per byte
            //    for (int y = 0; y < accessor.Height; y++)
            //    {
            //        var rowSpan = accessor.GetRowSpan(y);
            //        byte currentByte = 0;
            //        int bitCount = 0;

            //        foreach (var pixel in rowSpan)
            //        {
            //            var gray = (byte)((pixel.R + pixel.G + pixel.B) / 3);
            //            var bits = (byte)(gray >> 6); // 0-3 range
            //            currentByte = (byte)((currentByte << 2) | bits);
            //            bitCount += 2;

            //            if (bitCount == 8)
            //            {
            //                ms.WriteByte(currentByte);
            //                currentByte = 0;
            //                bitCount = 0;
            //            }
            //        }
            //    }
            //}
            //else 
            if (displayType == "BW") // FIXME constant
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
            else if (displayType == "3C") // FIXME constant
            {
                // 3-color (black, white, red/yellow) - dual buffers per row
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
                        // BW bit: 0 = black, 1 = white
                        var bwBit = (byte)((pixel.R + pixel.G + pixel.B) / 3 > 128 ? 1 : 0);

                        // Color bit: 0 = red/yellow (override), 1 = use B&W
                        var colorBit = (byte)(pixel.R > 128 && pixel.B < 128 ? 0 : 1);

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

    private static string ComputeSHA1(byte[] data)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    // TODO: Implement MQTT update methods
    public void UpdateMqtt(string key, object value, bool forced = false)
    {
        if (!_displayService.GetConfigBool("mqtt"))
            return;

        // TODO: Implement MQTT publishing
        _logger.LogDebug("MQTT update: {Key} = {Value}", key, value);
    }

    public void DisconnectMqtt()
    {
        if (!_displayService.GetConfigBool("mqtt"))
            return;

        // TODO: Implement MQTT disconnection
        _logger.LogDebug("Disconnecting MQTT");
    }
}

// Supporting classes
public class PageViewModel
{
    public required Display Display { get; set; }
    public required DateTime Date { get; set; }
    public required Dictionary<string, string> CssColors { get; set; }

    // All components are lazy
    private Lazy<PortalIconsComponent>? _portalIconsComponent;
    private Lazy<CalendarComponent>? _calendarComponent;
    private Lazy<WeightComponent>? _weightComponent;

    // Component instances
    // FIXME make all mandatory?
    public PortalIconsComponent? PortalIcons => _portalIconsComponent?.Value;
    public CalendarComponent? Calendar => _calendarComponent?.Value;
    public WeightComponent? Weight => _weightComponent?.Value;

    internal void InitializeComponents(
        Func<PortalIconsComponent> portalIconsFactory,
        Func<CalendarComponent> calendarFactory,
        Func<WeightComponent> weightFactory)
    {
        _portalIconsComponent = new Lazy<PortalIconsComponent>(portalIconsFactory);
        _calendarComponent = new Lazy<CalendarComponent>(calendarFactory);
        _weightComponent = new Lazy<WeightComponent>(weightFactory);
    }
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
    public string ColormapName { get; set; } = "none";
    public List<string> ColormapColors { get; set; } = new();
    public string Format { get; set; } = "png";
    public string DisplayType { get; set; } = "BW"; // FIXME constant
}

public class BitmapResult
{
    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = "image/png";
    public Dictionary<string, string>? Headers { get; set; }
}
