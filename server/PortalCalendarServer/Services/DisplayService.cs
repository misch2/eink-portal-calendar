using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Models.POCOs.Bitmap;
using PortalCalendarServer.Models.POCOs.Board;
using PortalCalendarServer.Services.BackgroundJobs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PortalCalendarServer.Services;

public class DisplayService(
    CalendarContext context,
    ILogger<DisplayService> logger,
    IConfiguration _configuration,
    ImageRegenerationService imageRegenerationService) : IDisplayService
{
    public IEnumerable<Display> GetAllDisplays()
    {
        return context.Displays
            .OrderBy(d => d.Id)
            .ToList();
    }

    public Display? GetDisplayById(int displayNumber)
    {
        var display = context.Displays
            .Include(d => d.Configs)
            .Include(d => d.DisplayType)
            .Include(d => d.ColorVariant)
            .Include(d => d.Theme)
            .FirstOrDefault(d => d.Id == displayNumber);
        return display;
    }

    public Display GetDefaultDisplay()
    {
        return context.Displays.Single(d => d.Id == 0);
    }

    public List<DisplayType> GetDisplayTypes()
    {
        return context.DisplayTypes
            .OrderBy(dt => dt.SortOrder)
            .ToList();
    }

    public List<ColorVariant> GetColorVariants()
    {
        return context.ColorVariants
            .OrderBy(cv => cv.DisplayTypeCode)
            .ThenBy(cv => cv.SortOrder)
            .ToList();
    }

    public List<DitheringType> GetDitheringTypes()
    {
        return context.DitheringTypes
            .OrderBy(dt => dt.SortOrder)
            .ToList();
    }

    public TimeZoneInfo GetTimeZoneInfo(Display display)
    {
        var tzname = GetConfig(display, "timezone");
        if (tzname is null)
        {
            tzname = "UTC";
            logger.LogWarning("Timezone not set for display {DisplayId}, defaulting to {tzname}", display.Id, tzname);
        }
        return TimeZoneInfo.FindSystemTimeZoneById(tzname);
    }

    public CultureInfo GetDateCultureInfo(Display display)
    {
        var cultureName = GetConfig(display, "date_culture");
        if (cultureName is null)
        {
            logger.LogWarning("Date culture not set for display {DisplayId}, defaulting to invariant culture", display.Id);
            return CultureInfo.InvariantCulture;
        }
        return new CultureInfo(cultureName);
    }

    /// <summary>
    /// Get configuration value for a display, with fallback to default display (ID = 0)
    /// </summary>
    public string? GetConfig(Display display, string name)
    {
        // 1. real value (empty string usually means "unset" in HTML form)
        var value = GetConfigWithoutDefaults(display, name);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        // 2. default value (modifiable)
        var default_value = GetConfigDefaultsOnly(name);
        if (!string.IsNullOrEmpty(default_value))
        {
            return default_value;
        }

        return null;
    }

    /// <summary>
    /// Get configuration value without checking defaults (only for this specific display)
    /// </summary>
    public string? GetConfigWithoutDefaults(Display display, string name)
    {
        var config = display.Configs?.FirstOrDefault(c => c.Name == name);
        return config?.Value;
    }

    /// <summary>
    /// Get configuration value from default display only (ID = 0)
    /// </summary>
    public string? GetConfigDefaultsOnly(string name)
    {
        var defaultConfig = GetDefaultDisplay().Configs?.FirstOrDefault(c => c.Name == name);

        return defaultConfig?.Value;
    }

    /// <summary>
    /// Get configuration value as boolean
    /// </summary>
    public bool GetConfigBool(Display display, string name, bool defaultValue = false)
    {
        var value = GetConfig(display, name);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get configuration value as integer
    /// </summary>
    public int? GetConfigInt(Display display, string name)
    {
        var value = GetConfig(display, name);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (int.TryParse(value, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Get configuration value as double
    /// </summary>
    public double? GetConfigDouble(Display display, string name)
    {
        var value = GetConfig(display, name);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Set configuration value for a display
    /// </summary>
    public void SetConfig(Display display, string name, string value)
    {
        var config = context.Configs
            .FirstOrDefault(c => c.DisplayId == display.Id && c.Name == name);

        if (config != null)
        {
            config.Value = value;
            context.Update(config);
        }
        else
        {
            context.Configs.Add(new Config
            {
                DisplayId = display.Id,
                Name = name,
                Value = value
            });
        }
    }

    public void EnqueueImageRegenerationRequest(Display display)
    {
        // Enqueue image regeneration in background
        if (display.IsDefault())
        {
            logger.LogWarning("Skipping image regeneration request for default display (ID = 0)");
            return;
        }
        // Remove cached image first
        // FIXME FIXME foreach (var fileName in )
        imageRegenerationService.EnqueueRequest(display.Id);
    }

    public void EnqueueAllImageRegenerationRequest()
    {
        var displays = GetAllDisplays().Where(d => !d.IsDefault()).ToList();
        foreach (var display in displays)
        {
            EnqueueImageRegenerationRequest(display);
        }
    }

    public int GetMissedConnects(Display display)
    {
        return GetConfigInt(display, "_missed_connects") ?? 0;
    }

    public DateTime? GetLastVisit(Display display)
    {
        var lastVisitStr = GetConfig(display, "_last_visit");
        if (string.IsNullOrEmpty(lastVisitStr))
        {
            return null;
        }

        if (DateTime.TryParse(lastVisitStr, null, DateTimeStyles.RoundtripKind, out var lastVisit))
        {
            return lastVisit.ToUniversalTime();
        }

        return null;
    }

    public decimal? GetVoltage(Display display)
    {
        var voltage = GetConfigDouble(display, "_last_voltage");
        if (!voltage.HasValue)
        {
            return null;
        }

        return Math.Round((decimal)voltage.Value, 2);
    }

    public decimal? GetBatteryPercent(Display display)
    {
        var min = GetConfigDouble(display, "_min_linear_voltage");
        var max = GetConfigDouble(display, "_max_linear_voltage");
        var voltage = GetVoltage(display);
        if (!min.HasValue || !max.HasValue || !voltage.HasValue)
        {
            return null;
        }

        var minDecimal = (decimal)min.Value;
        var maxDecimal = (decimal)max.Value;

        if (minDecimal == 0 || maxDecimal == 0)
        {
            return null;
        }

        var percentage = 100 * (voltage.Value - minDecimal) / (maxDecimal - minDecimal);
        percentage = Math.Max(0, Math.Min(100, percentage)); // Clip to 0-100

        return Math.Round(percentage, 1);
    }

    private DateTime GetNextWakeupTimeForDateTime(string schedule, DateTime dt, TimeZoneInfo timeZone)
    {
        // By default wake up tomorrow (truncate to day and add 1 day)
        var defaultDate = dt.Date.AddDays(1);

        // No schedule, wake up tomorrow (truncate to day and add 1 day)
        if (string.IsNullOrWhiteSpace(schedule))
        {
            return defaultDate;
        }

        // Crontab definitions are in the same time zone as the display
        // Parse cron schedule and find next occurrence
        try
        {
            var cronExpression = Cronos.CronExpression.Parse(schedule);
            var nextOccurrence = cronExpression.GetNextOccurrence(dt, timeZone);

            if (nextOccurrence.HasValue)
            {
                return nextOccurrence.Value;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid crontab schedule '{Schedule}', defaulting to tomorrow", schedule);
            return defaultDate;
        }

        // Fallback: wake up tomorrow if no next occurrence found
        logger.LogWarning("No next occurrence found for cron schedule '{Schedule}', defaulting to tomorrow", schedule);
        return defaultDate;
    }

    public WakeUpInfo GetNextWakeupTime(Display display, DateTime? optionalNow = null)
    {
        var now = optionalNow ?? DateTime.UtcNow;
        var timeZone = GetTimeZoneInfo(display);
        var schedule = GetConfig(display, "wakeup_schedule") ?? string.Empty;

        var nextWakeup = GetNextWakeupTimeForDateTime(schedule, now, timeZone);

        // If nextEvent is too close to now, it should return the next-next event
        // to avoid displays waking up multiple times due to inaccurate clocks
        var minSleepMinutes = GetConfigInt(display, "minimal_sleep_time_minutes") ?? 5;
        var diffSeconds = (nextWakeup - now).TotalSeconds;

        if (diffSeconds <= minSleepMinutes * 60)
        {
            nextWakeup = GetNextWakeupTimeForDateTime(schedule, nextWakeup.AddSeconds(1), timeZone);
        }

        var sleepInSeconds = (int)(nextWakeup - now).TotalSeconds;

        return new WakeUpInfo
        {
            NextWakeup = nextWakeup,
            SleepInSeconds = sleepInSeconds,
            Schedule = schedule
        };
    }

    public void ResetMissedConnectsCount(Display display)
    {
        SetConfig(display, "_missed_connects", "0");
        context.SaveChanges();
    }

    public void IncreaseMissedConnectsCount(Display display, DateTime expectedTimeOfConnect)
    {
        var previousExpectedTime = GetConfig(display, "_last_expected_time_of_connect");
        var expectedTimeStr = expectedTimeOfConnect.ToString("O");

        // Skip if this is the same expected time (avoid duplicate increments)
        if (previousExpectedTime == expectedTimeStr)
        {
            logger.LogDebug(
                "Skipping increase_missed_connects_count for display {DisplayId} because expected time hasn't changed",
                display.Id);
            return;
        }

        SetConfig(display, "_last_expected_time_of_connect", expectedTimeStr);

        var currentCount = GetMissedConnects(display);
        SetConfig(display, "_missed_connects", (currentCount + 1).ToString());

        context.SaveChanges();

        logger.LogWarning(
            "Increased missed connects count for display {DisplayId} to {Count} (expected at {ExpectedTime})",
            display.Id, currentCount + 1, expectedTimeOfConnect);
    }

    public void UpdateRenderInfo(Display display, DateTime renderedAt, string? renderErrors)
    {
        display.RenderedAt = renderedAt;
        display.RenderErrors = renderErrors;
        context.Update(display);
        context.SaveChanges();
    }

    public BitmapResult ConvertExistingWebSnapshot(Display display, BitmapOptions options)
    {
        logger.LogDebug("Converting pre-generated bitmap");

        var imagePath = RawWebSnapshotFileName(display);
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

            var palette = new List<Color>();
            palette.AddRange(options.ColormapColors);
            quantizer = new PaletteQuantizer(palette.ToArray(), quantizerOptions);

            img.Mutate(x => x.Quantize(quantizer));
        }

        // Save intermediate bitmap for debugging purposes
        var intermediatePath = DisplayIntermediateImageName(display);
        img.SaveAsPng(intermediatePath);

        // Generate output based on format
        if (options.Format == OutputFormat.Png)
        {
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return new BitmapResult { Data = ms.ToArray(), ContentType = "image/png" };
        }
        else if (options.Format == OutputFormat.EpaperSpecific)
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
    /// <remarks>
    /// Works with quantized images that have already been converted to the exact palette.
    /// </remarks>
    private byte[] _convertToEpaperFormat(Image<Rgba32> img, ColorVariant colorVariant)
    {
        using var ms = new MemoryStream();

        var displayType = colorVariant.DisplayType
            ?? throw new InvalidOperationException("ColorVariant has no associated DisplayType");

        // Process each row of pixels
        img.ProcessPixelRows(accessor =>
        {
            // The image is already quantized to the exact palette, so classify
            // each pixel by its RGB values into one of the known e-paper colors.

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
            else if (displayType.Code == "3C") // FIXME constant
            {
                // 3-color (black, white, red/yellow) - dual buffers per row
                var epdColors = colorVariant.EpdColors.ToArray();
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
                        var detectedColor = ClassifyPixelColor(pixel, epdColors);

                        // Dual-buffer encoding:
                        //   mono buffer | color buffer | result
                        //   -----------   ------------   ----
                        //       0              1         black
                        //       1              1         white
                        //       0              0         red   (or the single accent color for 3C)
                        //       1              0         red   (or the single accent color for 3C)
                        byte bwBit = 0;
                        byte colorBit = 0;
                        if (detectedColor.IsWhite)
                        {
                            bwBit = 1; colorBit = 1;
                        }
                        else if (detectedColor.IsBlack)
                        {
                            bwBit = 0; colorBit = 1;
                        }
                        else if (detectedColor.IsRed || detectedColor.IsYellow)
                        {
                            bwBit = 1; colorBit = 0;
                        }

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
            else if (displayType.Code == "4C") // FIXME constant
            {
                // 4-color (black, white, red, yellow) - single buffer with 2 bits per pixel
                var epdColors = colorVariant.EpdColors.ToArray();
                for (int y = 0; y < accessor.Height; y++)
                {
                    var rowSpan = accessor.GetRowSpan(y);
                    var bufferNative = new List<byte>();
                    byte byteNative = 0;
                    int bitCount = 0;

                    foreach (var pixel in rowSpan)
                    {
                        var detectedColor = ClassifyPixelColor(pixel, epdColors);
                        byte bufferPixel = 0;
                        //if (!(color_data & 0x80)) out_data |= black_data & 0x80 ? 0x03 : 0x02; // red or yellow
                        //else out_data |= black_data & 0x80 ? 0x01 : 0x00; // white or black

                        if (detectedColor.IsWhite)
                        {
                            bufferPixel = 0b01;
                        }
                        else if (detectedColor.IsBlack)
                        {
                            bufferPixel = 0b00;
                        }
                        else if (detectedColor.IsRed)
                        {
                            bufferPixel = 0b11;
                        }
                        else if (detectedColor.IsYellow)
                        {
                            bufferPixel = 0b10;
                        }

                        byteNative = (byte)((byteNative << 2) | bufferPixel);
                        bitCount += 2;

                        if (bitCount == 8)
                        {
                            bufferNative.Add(byteNative);
                            byteNative = 0;
                            bitCount = 0;
                        }
                    }

                    // Write native buffer for this row
                    foreach (var b in bufferNative)
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
    /// Classify a quantized pixel into the closest e-paper color.
    /// The image is expected to already be quantized to the exact palette
    /// </summary>
    private static EpdColor ClassifyPixelColor(Rgba32 pixel, EpdColor[] epdColors) // FIXME byref!
    {
        for (int i = 0; i < epdColors.Length; i++)
        {
            var c = epdColors[i].EpdPreviewRgba32Value;
            if (pixel.R == c.R && pixel.G == c.G && pixel.B == c.B)
            {
                return epdColors[i];
            }
        }
        throw new InvalidOperationException($"Pixel color #{pixel.R:X2}{pixel.G:X2}{pixel.B:X2} not found in EPD color palette");
    }

    private static string ComputeSHA1(byte[] data)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public string RawWebSnapshotFileName(Display display)
    {
        var imagePath = _configuration["Paths:GeneratedImages"]
            ?? throw new InvalidOperationException("GeneratedImages path is not configured");

        var ret = Path.Combine(imagePath, $"display-{display.Id}-raw-web-snapshot.png");

        return ret;
    }

    private string DisplayIntermediateImageName(Display display)
    {
        var imagePath = _configuration["Paths:GeneratedImages"]
            ?? throw new InvalidOperationException("GeneratedImages path is not configured");

        var ret = Path.Combine(imagePath, $"display-{display.Id}-intermediate.png");

        return ret;
    }

    public BitmapResult ConvertExistingRawBitmap( // FIXME name and purpose, this is for controllers
            int displayId,
            OutputFormat format,
            int? rotate = null,
            string? flip = null)
    {
        var ret = new BitmapResult();

        var display = GetDisplayById(displayId);
        if (display == null)
        {
            ret.ErrorMessage = "Display not found";
            return ret;
        }
        if (display.RenderedAt == null)
        {
            ret.ErrorMessage = "No rendered bitmap available for this display yet";
            return ret;
        }
        if (display.DisplayType == null)
        {
            ret.ErrorMessage = "Display type information is missing for this display";
            return ret;
        }

        rotate ??= display.Rotation;
        flip ??= "";

        var color_palette = display.ColorPalette(true); // FIXME make the ColorPalette return only real (preview) colors?
        if (color_palette.Count == 0)
        {
            throw new InvalidOperationException($"No colors defined for display {display.Id}");
        }

        var bitmapOptions = new BitmapOptions
        {
            Rotate = rotate!.Value,
            Flip = flip,
            Gamma = display.Gamma!.Value,
            NumColors = display.DisplayType!.NumColors,
            ColormapColors = color_palette,
            Format = format,
            DisplayType = display.DisplayType!,
            DitheringType = display.DitheringTypeCode
        };

        ret = ConvertExistingWebSnapshot(display, bitmapOptions);
        return ret;
    }
}
