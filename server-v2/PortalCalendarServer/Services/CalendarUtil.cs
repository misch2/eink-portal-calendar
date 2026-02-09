using PortalCalendarServer.Models;
using System.Security.Cryptography;
using System.Text;

namespace PortalCalendarServer.Services;

public class CalendarUtil
{
    private readonly ILogger<CalendarUtil> _logger;
    private readonly CalendarContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly DisplayService _displayService;
    private readonly Display _display;
    private readonly int _minimalCacheExpiry;

    // Portal icon constants
    private static readonly string[] PortalIcons = new[]
    {
        "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9", "a10",
        "a11", "a12", "a13", "a14", "a15", "b11", "b14", "c3", "c4",
        "c7", "d2", "d3", "d4", "d5", "e1", "e2", "e4"
    };

    private static readonly Dictionary<string, string> IconNameToFilename = new()
    {
        ["CUBE_DISPENSER"] = "a1",
        ["CUBE_HAZARD"] = "a2",
        ["PELLET_HAZARD"] = "a3",
        ["PELLET_CATCHER"] = "a4",
        ["FLING_ENTER"] = "a5",
        ["FLING_EXIT"] = "a6",
        ["TURRET_HAZARD"] = "a7",
        ["DIRTY_WATER"] = "a8",
        ["WATER_HAZARD"] = "a9",
        ["CAKE"] = "a10",
        ["LASER_REDIRECTION"] = "c3",
        ["CUBE_BUTTON"] = "d5",
        ["BLADES_HAZARD"] = "e4",
        ["BRIDGE_SHIELD"] = "d2",
        ["FAITH_PLATE"] = "e1",
        ["LASER_HAZARD"] = "d3",
        ["LASER_SENSOR"] = "c4",
        ["LIGHT_BRIDGE"] = "e2",
        ["PLAYER_BUTTON"] = "d4",
    };

    // Chamber icons by day (31 days worth of Portal 1 & 2 chamber configurations)
    private static readonly List<List<(string name, bool enabled)>> ChamberIconsByDay = InitializeChamberIcons();

    public CalendarUtil(
        ILogger<CalendarUtil> logger,
        CalendarContext context,
        IWebHostEnvironment environment,
        DisplayService displayService,
        Display display,
        int minimalCacheExpiry = 0)
    {
        _logger = logger;
        _context = context;
        _environment = environment;
        _displayService = displayService;
        _display = display;
        _minimalCacheExpiry = minimalCacheExpiry;
    }

    public string ImageName => $"generated_images/current_calendar_{_display.Id}.png";

    /// <summary>
    /// Generate HTML data for a specific date
    /// </summary>
    public CalendarViewModel HtmlForDate(DateTime date, bool previewColors = false)
    {
        // Keep the calendar random, but consistent for any given day
        var seed = int.Parse(date.ToString("yyyyMMdd"));
        var random = new Random(seed);

        var (icons, todayEvents, nearestEvents, nearestEventsGrouped, hasCalendarEntries) = 
            GetCalendarComponent(date);

        // TODO: Implement weather, Google Fit, etc. components
        // var (currentWeather, forecast) = GetWeatherComponent(date);
        // var (weightSeries, lastWeight) = GetGoogleFitComponent(date);
        // var svatkyApi = GetSvatkyApiComponent(date);
        // var xkcd = GetXkcdComponent(date);

        return new CalendarViewModel
        {
            Display = _display,
            Date = date,
            Icons = icons,
            TodayEvents = todayEvents,
            NearestEvents = nearestEvents,
            NearestEventsGrouped = nearestEventsGrouped,
            HasCalendarEntries = hasCalendarEntries,
            // CurrentWeather = currentWeather,
            // Forecast = forecast,
            // LastWeight = lastWeight,
            // WeightSeries = weightSeries,
            
            // EPD Colors
            Colors = _display.CssColorMap(previewColors),
        };
    }

    /// <summary>
    /// Get calendar component (icons and events)
    /// </summary>
    private (List<IconViewModel> icons, List<CalendarEvent> todayEvents, 
             List<CalendarEvent> nearestEvents, Dictionary<string, List<CalendarEvent>> grouped, 
             bool hasEntries) GetCalendarComponent(DateTime date)
    {
        var todayEvents = new List<CalendarEvent>();
        var nearestEvents = new List<CalendarEvent>();

        // Load calendar events from up to 3 ICS calendars
        for (int calendarNo = 1; calendarNo <= 3; calendarNo++)
        {
            var enabled = _displayService.GetConfigBool(_display, $"web_calendar{calendarNo}");
            if (!enabled)
                continue;

            var url = _displayService.GetConfig(_display, $"web_calendar_ics_url{calendarNo}");
            if (string.IsNullOrEmpty(url))
                continue;

            try
            {
                // TODO: Implement ICS calendar integration
                // var calendar = new ICalIntegration(url, _display, _minimalCacheExpiry);
                // todayEvents.AddRange(calendar.GetEventsBetween(date.Date, date.Date.AddDays(1).AddSeconds(-1)));
                // nearestEvents.AddRange(calendar.GetEventsBetween(date.Date, date.Date.AddMonths(12)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading calendar {CalendarNo}", calendarNo);
            }
        }

        todayEvents = todayEvents.OrderBy(e => e.StartTime).ToList();
        nearestEvents = nearestEvents.OrderBy(e => e.StartTime).ToList();
        var hasCalendarEntries = todayEvents.Any();

        // Group nearest events by date
        var nearestEventsGrouped = nearestEvents
            .GroupBy(e => e.StartTime.Date.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Generate icons
        var icons = GenerateIcons(date, hasCalendarEntries);

        return (icons, todayEvents, nearestEvents, nearestEventsGrouped, hasCalendarEntries);
    }

    /// <summary>
    /// Generate Portal icons for the display
    /// </summary>
    private List<IconViewModel> GenerateIcons(DateTime date, bool hasCalendarEntries)
    {
        var icons = new List<IconViewModel>();
        var totallyRandom = _displayService.GetConfigBool(_display, "totally_random_icon");

        if (!totallyRandom)
        {
            // Use Portal chamber icon sets based on day of month
            var dayIndex = date.Day - 1; // 0-based index
            if (dayIndex < ChamberIconsByDay.Count)
            {
                var chamberSet = ChamberIconsByDay[dayIndex];
                foreach (var (name, enabled) in chamberSet)
                {
                    if (IconNameToFilename.TryGetValue(name, out var filename))
                    {
                        icons.Add(new IconViewModel
                        {
                            Name = filename,
                            Grayed = !enabled
                        });
                    }
                }

                // Limit icons if calendar entries exist
                if (hasCalendarEntries)
                {
                    var maxIcons = int.Parse(_displayService.GetConfig(_display, "max_icons_with_calendar") ?? "5");
                    if (icons.Count > maxIcons)
                    {
                        icons = icons.Take(maxIcons).ToList();
                    }
                }
            }
        }
        else
        {
            // Random icon set
            var seed = int.Parse(date.ToString("yyyyMMdd"));
            var random = new Random(seed);
            var grayProbability = 0.25;

            var shuffled = PortalIcons.OrderBy(_ => random.Next()).ToList();
            foreach (var name in shuffled)
            {
                icons.Add(new IconViewModel
                {
                    Name = name,
                    Grayed = random.NextDouble() < grayProbability
                });
            }

            // Duplicate if needed
            while (icons.Count < 16)
            {
                icons.AddRange(icons.ToList());
            }

            var minIcons = int.Parse(_displayService.GetConfig(_display, "min_random_icons") ?? "4");
            var maxIcons = hasCalendarEntries
                ? int.Parse(_displayService.GetConfig(_display, "max_icons_with_calendar") ?? "5")
                : int.Parse(_displayService.GetConfig(_display, "max_random_icons") ?? "10");

            minIcons = Math.Min(minIcons, maxIcons);
            var iconCount = minIcons + random.Next(maxIcons - minIcons + 1);
            icons = icons.Take(iconCount).ToList();
        }

        return icons;
    }

    /// <summary>
    /// Generate bitmap image from source PNG
    /// </summary>
    public BitmapResult GenerateBitmap(BitmapOptions options)
    {
        _logger.LogDebug("Producing bitmap");

        var imagePath = Path.Combine(_environment.ContentRootPath, "..", ImageName);
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        // TODO: Implement image processing using ImageSharp or SkiaSharp
        // This is a placeholder - full implementation would require:
        // 1. Load image
        // 2. Crop if needed
        // 3. Rotate if needed
        // 4. Flip if needed
        // 5. Apply gamma correction
        // 6. Convert to palette/grayscale
        // 7. Export in requested format (PNG, epaper_native, etc.)

        throw new NotImplementedException("Bitmap generation requires image processing library (ImageSharp/SkiaSharp)");

        // Example structure for when implemented:
        /*
        using var image = Image.Load(imagePath);
        
        // Crop if oversized
        if (image.Height > _display.VirtualHeight())
        {
            image.Mutate(x => x.Crop(new Rectangle(0, 0, _display.VirtualWidth(), _display.VirtualHeight())));
        }

        // Rotate
        if (options.Rotate != 0)
        {
            image.Mutate(x => x.Rotate(options.Rotate * 90));
        }

        // Apply gamma, palette conversion, etc.
        
        if (options.Format == "png")
        {
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return new BitmapResult { Data = ms.ToArray(), ContentType = "image/png" };
        }
        else if (options.Format == "epaper_native")
        {
            var bitmap = ConvertToEpaperFormat(image, options.DisplayType);
            var checksum = ComputeSHA1(bitmap);
            var output = Encoding.ASCII.GetBytes("MM\n") 
                .Concat(Encoding.ASCII.GetBytes(checksum + "\n"))
                .Concat(bitmap)
                .ToArray();
            
            return new BitmapResult 
            { 
                Data = output, 
                ContentType = "application/octet-stream" 
            };
        }
        */
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
        if (!_displayService.GetConfigBool(_display, "mqtt"))
            return;

        // TODO: Implement MQTT publishing
        _logger.LogDebug("MQTT update: {Key} = {Value}", key, value);
    }

    public void DisconnectMqtt()
    {
        if (!_displayService.GetConfigBool(_display, "mqtt"))
            return;

        // TODO: Implement MQTT disconnection
        _logger.LogDebug("Disconnecting MQTT");
    }

    private static List<List<(string, bool)>> InitializeChamberIcons()
    {
        // Portal 1 and Portal 2 chamber icon configurations
        // This is a static data structure matching the Perl version
        return new List<List<(string, bool)>>
        {
            // Day 1 - P1 Chamber 1
            new() { ("CUBE_DISPENSER", false), ("CUBE_HAZARD", false), ("PELLET_HAZARD", false), ("PELLET_CATCHER", false), ("WATER_HAZARD", false),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", false), ("CAKE", false) },
            
            // Day 2 - P1 Chamber 4
            new() { ("CUBE_DISPENSER", true), ("CUBE_HAZARD", true), ("PELLET_HAZARD", false), ("PELLET_CATCHER", false), ("WATER_HAZARD", false),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", false), ("CAKE", false) },

            // Day 3 - P1 Chamber 5
            new() { ("CUBE_DISPENSER", false), ("CUBE_HAZARD", true), ("PELLET_HAZARD", false), ("PELLET_CATCHER", false), ("WATER_HAZARD", false),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", false), ("CAKE", false) },

            // Day 4 - P1 Chamber 6
            new() { ("CUBE_DISPENSER", false), ("CUBE_HAZARD", false), ("PELLET_HAZARD", true), ("PELLET_CATCHER", true), ("WATER_HAZARD", false),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", false), ("CAKE", false) },

            // Day 5 - P1 Chamber 8
            new() { ("CUBE_DISPENSER", false), ("CUBE_HAZARD", false), ("PELLET_HAZARD", true), ("PELLET_CATCHER", true), ("WATER_HAZARD", true),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", true), ("CAKE", false) },

            // Day 6 - P1 Chamber 9
            new() { ("CUBE_DISPENSER", true), ("CUBE_HAZARD", true), ("PELLET_HAZARD", false), ("PELLET_CATCHER", false), ("WATER_HAZARD", false),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", false), ("CAKE", false) },

            // Day 7 - P1 Chamber 10
            new() { ("CUBE_DISPENSER", false), ("CUBE_HAZARD", false), ("PELLET_HAZARD", false), ("PELLET_CATCHER", false), ("WATER_HAZARD", false),
                    ("FLING_ENTER", true), ("FLING_EXIT", true), ("TURRET_HAZARD", false), ("DIRTY_WATER", false), ("CAKE", false) },

            // Day 8 - P1 Chamber 11
            new() { ("CUBE_DISPENSER", false), ("CUBE_HAZARD", false), ("PELLET_HAZARD", true), ("PELLET_CATCHER", true), ("WATER_HAZARD", true),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", true), ("CAKE", false) },

            // Day 9 - P1 Chamber 12
            new() { ("CUBE_DISPENSER", true), ("CUBE_HAZARD", true), ("PELLET_HAZARD", false), ("PELLET_CATCHER", false), ("WATER_HAZARD", false),
                    ("FLING_ENTER", true), ("FLING_EXIT", true), ("TURRET_HAZARD", false), ("DIRTY_WATER", false), ("CAKE", false) },

            // Day 10 - P1 Chamber 13
            new() { ("CUBE_DISPENSER", false), ("CUBE_HAZARD", true), ("PELLET_HAZARD", true), ("PELLET_CATCHER", true), ("WATER_HAZARD", false),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", false), ("CAKE", false) },

            // Day 11 - P1 Chamber 14
            new() { ("CUBE_DISPENSER", false), ("CUBE_HAZARD", true), ("PELLET_HAZARD", true), ("PELLET_CATCHER", true), ("WATER_HAZARD", true),
                    ("FLING_ENTER", true), ("FLING_EXIT", true), ("TURRET_HAZARD", false), ("DIRTY_WATER", true), ("CAKE", false) },

            // Day 12 - P1 Chamber 15
            new() { ("CUBE_DISPENSER", false), ("CUBE_HAZARD", false), ("PELLET_HAZARD", true), ("PELLET_CATCHER", true), ("WATER_HAZARD", true),
                    ("FLING_ENTER", true), ("FLING_EXIT", true), ("TURRET_HAZARD", false), ("DIRTY_WATER", true), ("CAKE", false) },

            // Day 13 - P1 Chamber 16
            new() { ("CUBE_DISPENSER", true), ("CUBE_HAZARD", true), ("PELLET_HAZARD", false), ("PELLET_CATCHER", false), ("WATER_HAZARD", false),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", true), ("DIRTY_WATER", false), ("CAKE", false) },

            // Day 14 - P1 Chamber 17
            new() { ("CUBE_DISPENSER", true), ("CUBE_HAZARD", true), ("PELLET_HAZARD", true), ("PELLET_CATCHER", true), ("WATER_HAZARD", false),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", false), ("CAKE", false) },

            // Day 15 - P1 Chamber 18
            new() { ("CUBE_DISPENSER", true), ("CUBE_HAZARD", true), ("PELLET_HAZARD", true), ("PELLET_CATCHER", true), ("WATER_HAZARD", true),
                    ("FLING_ENTER", true), ("FLING_EXIT", true), ("TURRET_HAZARD", true), ("DIRTY_WATER", true), ("CAKE", false) },

            // Day 16 - P1 Chamber 19
            new() { ("CUBE_DISPENSER", false), ("CUBE_HAZARD", false), ("PELLET_HAZARD", true), ("PELLET_CATCHER", true), ("WATER_HAZARD", true),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", true), ("CAKE", true) },

            // Day 17 - P2 The Cold Boot Chamber 1
            new() { ("LASER_SENSOR", true), ("LASER_REDIRECTION", false), ("CUBE_DISPENSER", false), ("CUBE_BUTTON", false), ("CUBE_HAZARD", false),
                    ("PLAYER_BUTTON", false), ("WATER_HAZARD", false), ("TURRET_HAZARD", false), ("LASER_HAZARD", false), ("DIRTY_WATER", false) },

            // Day 18 - P2 The Cold Boot Chamber 2
            new() { ("CUBE_DISPENSER", true), ("CUBE_BUTTON", true), ("CUBE_HAZARD", true), ("PLAYER_BUTTON", false), ("WATER_HAZARD", false),
                    ("LASER_SENSOR", true), ("LASER_REDIRECTION", true), ("TURRET_HAZARD", false), ("LASER_HAZARD", false), ("DIRTY_WATER", false) },

            // Day 19 - P2 The Cold Boot Chamber 3
            new() { ("CUBE_DISPENSER", false), ("CUBE_BUTTON", false), ("CUBE_HAZARD", false), ("PLAYER_BUTTON", false), ("WATER_HAZARD", false),
                    ("LASER_SENSOR", true), ("LASER_REDIRECTION", true), ("TURRET_HAZARD", false), ("LASER_HAZARD", false), ("DIRTY_WATER", false) },

            // Day 20 - P2 The Cold Boot Chamber 4
            new() { ("CUBE_DISPENSER", true), ("CUBE_BUTTON", true), ("CUBE_HAZARD", true), ("PLAYER_BUTTON", false), ("WATER_HAZARD", true),
                    ("LASER_SENSOR", true), ("LASER_REDIRECTION", false), ("TURRET_HAZARD", false), ("LASER_HAZARD", false), ("DIRTY_WATER", false) },

            // Day 21 - P2 The Cold Boot Chamber 5
            new() { ("CUBE_DISPENSER", true), ("CUBE_BUTTON", true), ("CUBE_HAZARD", true), ("PLAYER_BUTTON", false), ("WATER_HAZARD", true),
                    ("FLING_ENTER", false), ("FLING_EXIT", false), ("FAITH_PLATE", true), ("TURRET_HAZARD", false), ("DIRTY_WATER", false) },

            // Day 22 - P2 The Cold Boot Chamber 7
            new() { ("CUBE_DISPENSER", true), ("CUBE_BUTTON", true), ("CUBE_HAZARD", true), ("WATER_HAZARD", false), ("FLING_ENTER", true),
                    ("FLING_EXIT", true), ("LASER_SENSOR", true), ("LASER_REDIRECTION", false), ("TURRET_HAZARD", false), ("DIRTY_WATER", false) },

            // Day 23 - P2 The Cold Boot Chamber 8
            new() { ("CUBE_DISPENSER", true), ("CUBE_BUTTON", true), ("CUBE_HAZARD", true), ("WATER_HAZARD", false), ("FLING_ENTER", false),
                    ("FLING_EXIT", false), ("LASER_SENSOR", true), ("LASER_REDIRECTION", true), ("TURRET_HAZARD", false), ("DIRTY_WATER", false) },

            // Day 24 - P2 The Return Chamber 9
            new() { ("CUBE_DISPENSER", true), ("CUBE_BUTTON", false), ("CUBE_HAZARD", true), ("PLAYER_BUTTON", false), ("WATER_HAZARD", false),
                    ("LASER_SENSOR", true), ("LASER_REDIRECTION", true), ("FAITH_PLATE", true), ("TURRET_HAZARD", false), ("DIRTY_WATER", false) },

            // Day 25 - P2 The Return Chamber 10
            new() { ("CUBE_DISPENSER", true), ("CUBE_BUTTON", true), ("CUBE_HAZARD", true), ("WATER_HAZARD", false), ("LASER_SENSOR", true),
                    ("LASER_REDIRECTION", true), ("FAITH_PLATE", true), ("FLING_ENTER", true), ("FLING_EXIT", true), ("DIRTY_WATER", false) },

            // Day 26 - P2 The Return Chamber 11
            new() { ("CUBE_DISPENSER", true), ("CUBE_BUTTON", true), ("CUBE_HAZARD", true), ("PLAYER_BUTTON", false), ("WATER_HAZARD", true),
                    ("LIGHT_BRIDGE", true), ("TURRET_HAZARD", false), ("BRIDGE_SHIELD", false), ("LASER_REDIRECTION", false), ("DIRTY_WATER", false) },

            // Day 27 - P2 The Return Chamber 13
            new() { ("CUBE_DISPENSER", false), ("CUBE_BUTTON", true), ("CUBE_HAZARD", true), ("PLAYER_BUTTON", false), ("WATER_HAZARD", false),
                    ("LIGHT_BRIDGE", false), ("TURRET_HAZARD", true), ("BRIDGE_SHIELD", false), ("LASER_HAZARD", false), ("DIRTY_WATER", false) },

            // Day 28 - P2 The Return Chamber 15
            new() { ("CUBE_DISPENSER", false), ("CUBE_BUTTON", true), ("CUBE_HAZARD", false), ("PLAYER_BUTTON", false), ("WATER_HAZARD", false),
                    ("LIGHT_BRIDGE", true), ("TURRET_HAZARD", true), ("BRIDGE_SHIELD", true), ("FAITH_PLATE", true), ("DIRTY_WATER", false) },

            // Day 29 - P2 The Return Chamber 16
            new() { ("CUBE_DISPENSER", false), ("CUBE_BUTTON", true), ("CUBE_HAZARD", false), ("PLAYER_BUTTON", true), ("WATER_HAZARD", false),
                    ("LASER_REDIRECTION", true), ("LASER_SENSOR", true), ("TURRET_HAZARD", true), ("LASER_HAZARD", true), ("DIRTY_WATER", false) },

            // Day 30 - P2 The Surprise Chamber 18
            new() { ("CUBE_DISPENSER", true), ("CUBE_BUTTON", false), ("CUBE_HAZARD", true), ("WATER_HAZARD", true), ("LIGHT_BRIDGE", true),
                    ("LASER_SENSOR", true), ("LASER_REDIRECTION", true), ("TURRET_HAZARD", true), ("BRIDGE_SHIELD", true), ("LASER_HAZARD", true) },

            // Day 31 - P2 The Surprise Chamber 19
            new() { ("CUBE_DISPENSER", false), ("CUBE_BUTTON", false), ("CUBE_HAZARD", false), ("PLAYER_BUTTON", false), ("LASER_SENSOR", true),
                    ("LASER_REDIRECTION", true), ("FAITH_PLATE", true), ("TURRET_HAZARD", true), ("LASER_HAZARD", true), ("DIRTY_WATER", false) },
        };
    }
}

// Supporting classes
public class CalendarViewModel
{
    public Display Display { get; set; } = null!;
    public DateTime Date { get; set; }
    public List<IconViewModel> Icons { get; set; } = new();
    public List<CalendarEvent> TodayEvents { get; set; } = new();
    public List<CalendarEvent> NearestEvents { get; set; } = new();
    public Dictionary<string, List<CalendarEvent>> NearestEventsGrouped { get; set; } = new();
    public bool HasCalendarEntries { get; set; }
    public Dictionary<string, string> Colors { get; set; } = new();
    
    // Google Fit / Weight tracking
    public decimal? LastWeight { get; set; }
    public List<WeightDataPoint>? WeightSeries { get; set; }
    
    // TODO: Add weather properties
    // public WeatherData? CurrentWeather { get; set; }
    // public List<WeatherForecast>? Forecast { get; set; }
}

public class IconViewModel
{
    public string Name { get; set; } = string.Empty;
    public bool Grayed { get; set; }
}

public class CalendarEvent
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool AllDay { get; set; }
    public double? DurationHours { get; set; }
}

public class WeightDataPoint
{
    public DateTime Date { get; set; }
    public decimal Weight { get; set; }
}

public class BitmapOptions
{
    public int Rotate { get; set; }
    public string Flip { get; set; } = string.Empty;
    public double Gamma { get; set; } = 1.0;
    public int NumColors { get; set; } = 256;
    public string ColormapName { get; set; } = "none";
    public List<string> ColormapColors { get; set; } = new();
    public string Format { get; set; } = "png";
    public string DisplayType { get; set; } = "BW";
}

public class BitmapResult
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "image/png";
    public Dictionary<string, string>? Headers { get; set; }
}
