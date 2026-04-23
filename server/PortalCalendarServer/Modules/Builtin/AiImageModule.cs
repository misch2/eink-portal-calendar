using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for generating images using an external AI service (OpenAI DALL-E).
/// Generated images are saved to a gallery for caching and user browsing.
/// </summary>
public class AiImageModule : IPortalModule
{
    public string ModuleId => "aiimage";
    public string? ConfigTabDisplayName => null;
    public string? ConfigPartialView => null;

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "aiimage_api_key",
        "aiimage_prompt",
        "aiimage_gallery_id",
        "aiimage_cache_hours",
        "aiimage_quality",
        "aiimage_style"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => [];

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date)
    {
        var displayService = services.GetRequiredService<IDisplayService>();
        var galleryService = services.GetRequiredService<IGalleryService>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var databaseCacheFactory = services.GetRequiredService<IDatabaseCacheServiceFactory>();
        var context = services.GetRequiredService<CalendarContext>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        return new AiImageComponent(displayService, galleryService, httpClientFactory, memoryCache, databaseCacheFactory, context, loggerFactory);
    }
}
