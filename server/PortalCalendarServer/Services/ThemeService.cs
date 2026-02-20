using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;

namespace PortalCalendarServer.Services;

public class ThemeService
{
    private readonly CalendarContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ThemeService> _logger;

    public ThemeService(
        CalendarContext context,
        IWebHostEnvironment environment,
        ILogger<ThemeService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active themes ordered by sort order
    /// </summary>
    public async Task<List<Theme>> GetActiveThemesAsync()
    {
        return await _context.Themes
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a theme by its ID
    /// </summary>
    public async Task<Theme?> GetThemeByIdAsync(int themeId)
    {
        return await _context.Themes
            .FirstOrDefaultAsync(t => t.Id == themeId && t.IsActive);
    }

    /// <summary>
    /// Gets the default theme (the first theme marked as default)
    /// </summary>
    public async Task<Theme> GetDefaultThemeAsync()
    {
        var defaultTheme = await _context.Themes
            .Where(t => t.IsDefault)
            .OrderBy(t => t.SortOrder)
            .FirstAsync();
        return defaultTheme;
    }
}
