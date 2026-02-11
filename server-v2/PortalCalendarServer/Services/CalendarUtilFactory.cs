using PortalCalendarServer.Data;
using PortalCalendarServer.Models;
using PortalCalendarServer.Services.PageGeneratorComponents;
using Microsoft.Extensions.Caching.Memory;

namespace PortalCalendarServer.Services;

/// <summary>
/// Factory for creating CalendarUtil instances with specific Display context
/// </summary>
public interface ICalendarUtilFactory
{
    PageGeneratorService Create(Display display, int minimalCacheExpiry = 0);
}

public class CalendarUtilFactory : ICalendarUtilFactory
{
    private readonly ILogger<PageGeneratorService> _logger;
    private readonly CalendarContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly DisplayService _displayService;
    private readonly Web2PngService _web2PngService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;

    public CalendarUtilFactory(
        ILogger<PageGeneratorService> logger,
        CalendarContext context,
        IWebHostEnvironment environment,
        DisplayService displayService,
        Web2PngService web2PngService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache
        )
    {
        _logger = logger;
        _context = context;
        _environment = environment;
        _displayService = displayService;
        _web2PngService = web2PngService;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
    }

    public PageGeneratorService Create(Display display, int minimalCacheExpiry = 0)
    {
        return new PageGeneratorService(
            _logger,
            _context,
            _environment,
            _displayService,
            _web2PngService,
            _httpClientFactory,
            _memoryCache,
            display,
            minimalCacheExpiry);
    }
}
