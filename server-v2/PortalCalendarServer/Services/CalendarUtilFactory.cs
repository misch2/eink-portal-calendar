using PortalCalendarServer.Models;

namespace PortalCalendarServer.Services;

/// <summary>
/// Factory for creating CalendarUtil instances with specific Display context
/// </summary>
public interface ICalendarUtilFactory
{
    CalendarUtil Create(Display display, int minimalCacheExpiry = 0);
}

public class CalendarUtilFactory : ICalendarUtilFactory
{
    private readonly ILogger<CalendarUtil> _logger;
    private readonly CalendarContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly DisplayService _displayService;
    private readonly Web2PngService _web2PngService;

    public CalendarUtilFactory(
        ILogger<CalendarUtil> logger,
        CalendarContext context,
        IWebHostEnvironment environment,
        DisplayService displayService,
        Web2PngService web2PngService)
    {
        _logger = logger;
        _context = context;
        _environment = environment;
        _displayService = displayService;
        _web2PngService = web2PngService;
    }

    public CalendarUtil Create(Display display, int minimalCacheExpiry = 0)
    {
        return new CalendarUtil(_logger, _context, _environment, _displayService, _web2PngService, display, minimalCacheExpiry);
    }
}
