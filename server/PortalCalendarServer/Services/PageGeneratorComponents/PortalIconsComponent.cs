namespace PortalCalendarServer.Services.PageGeneratorComponents;

using PortalCalendarServer.Models.DatabaseEntities;

public class PortalIconsComponent(
    IDisplayService displayService)
{
    // Portal icon constants
    private static readonly string[] PortalIcons =
    [
        "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9", "a10",
        "a11", "a12", "a13", "a14", "a15", "b11", "b14", "c3", "c4",
        "c7", "d2", "d3", "d4", "d5", "e1", "e2", "e4"
    ];

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

    /// <summary>
    /// Generate Portal icons for the display based on date and calendar settings
    /// </summary>
    public List<IconViewModel> GenerateIcons(Display display, DateTime date, bool hasCalendarEntries)
    {
        List<IconViewModel> icons;
        var totallyRandom = displayService.GetConfigBool(display, "totally_random_icon");

        if (!totallyRandom)
        {
            icons = GenerateChamberIcons(date);

            // Limit icons if calendar entries exist
            if (hasCalendarEntries)
            {
                var maxIcons = int.Parse(displayService.GetConfig(display, "max_icons_with_calendar") ?? "5");
                if (icons.Count > maxIcons)
                {
                    icons = icons.Take(maxIcons).ToList();
                }
            }
        }
        else
        {
            icons = GenerateRandomIcons(display, date, hasCalendarEntries);
        }

        return icons;
    }

    /// <summary>
    /// Generate icons based on Portal chamber configurations
    /// </summary>
    private static List<IconViewModel> GenerateChamberIcons(DateTime date)
    {
        var icons = new List<IconViewModel>();
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
        }

        return icons;
    }

    /// <summary>
    /// Generate random icons with consistent seed based on date
    /// </summary>
    private List<IconViewModel> GenerateRandomIcons(Display display, DateTime date, bool hasCalendarEntries)
    {
        var icons = new List<IconViewModel>();
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

        var minIcons = int.Parse(displayService.GetConfig(display, "min_random_icons") ?? "4");
        var maxIcons = hasCalendarEntries
            ? int.Parse(displayService.GetConfig(display, "max_icons_with_calendar") ?? "5")
            : int.Parse(displayService.GetConfig(display, "max_random_icons") ?? "10");

        minIcons = Math.Min(minIcons, maxIcons);
        var iconCount = minIcons + random.Next(maxIcons - minIcons + 1);
        icons = icons.Take(iconCount).ToList();

        return icons;
    }

    /// <summary>
    /// Initialize Portal 1 and Portal 2 chamber icon configurations for all 31 days
    /// </summary>
    private static List<List<(string, bool)>> InitializeChamberIcons()
    {
        return
        [
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
        ];
    }
}

public class IconViewModel
{
    public string Name { get; set; } = string.Empty;
    public bool Grayed { get; set; }
}
