using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Modules;
using PortalCalendarServer.Services.PageGeneratorComponents;
using SixLabors.ImageSharp;

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

        var outputPath = _displayService.RawWebSnapshotFileName(display);
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
    public WebImageComponent? WebImage => GetComponent<WebImageComponent>("webimage");
}
