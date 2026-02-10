using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models;
using PortalCalendarServer.Models.Constants;
using PortalCalendarServer.Models.Entities;

namespace PortalCalendarServer.Services
{
    public class DisplayService
    {
        private readonly CalendarContext _context;
        private readonly ILogger<DisplayService> _logger;
        private readonly ColorTypeRegistry _colorTypeRegistry;

        public DisplayService(
            CalendarContext context, 
            ILogger<DisplayService> logger,
            ColorTypeRegistry colorTypeRegistry)
        {
            _context = context;
            _logger = logger;
            _colorTypeRegistry = colorTypeRegistry;
        }


        public IEnumerable<Display> GetAllDisplays()
        {
            return _context.Displays
                .Include(d => d.Configs)
                .OrderBy(d => d.Id)
                .ToList();
        }

        public Display GetDefaultDisplay()
        {
            return _context.Displays
                .Include(d => d.Configs)
                .Single(d => d.Id == 0);
        }

        /// <summary>
        /// Get configuration value for a display, with fallback to default display (ID = 0)
        /// </summary>
        public string? GetConfig(Display display, string name, string defaultValue = "")
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
        /// Set configuration value for a display
        /// </summary>
        public void SetConfig(Display display, string name, string value)
        {
            var config = _context.Configs
                .FirstOrDefault(c => c.DisplayId == display.Id && c.Name == name);

            if (config != null)
            {
                config.Value = value;
                _context.Update(config);
            }
            else
            {
                _context.Configs.Add(new Config
                {
                    DisplayId = display.Id,
                    Name = name,
                    Value = value
                });
            }
        }

        /// <summary>
        /// Save all changes to the database
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public string GetColorTypePretty(Display display)
        {
            var colorType = _colorTypeRegistry.GetColorType(display.ColorType);
            return colorType?.PrettyName ?? display.ColorType;
        }

        public int GetNumColors(Display display)
        {
            var colorType = _colorTypeRegistry.GetColorType(display.ColorType);
            return colorType?.NumColors ?? 256;
        }

        public List<string> GetColorPalette(Display display, bool forPreview)
        {
            var colorType = _colorTypeRegistry.GetColorType(display.ColorType);
            return colorType?.GetColorPalette(forPreview) ?? new List<string>();
        }
    }
}
