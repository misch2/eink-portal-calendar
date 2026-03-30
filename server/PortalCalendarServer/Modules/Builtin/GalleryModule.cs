using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

public class GalleryModule : IPortalModule
{
    public const int MaxGalleries = 5;

    public string ModuleId => "gallery";
    public string? ConfigTabDisplayName => null;
    public string? ConfigPartialView => null;

    public IReadOnlyList<string> OwnedConfigKeys { get; } = BuildConfigKeys();
    public IReadOnlyList<string> CheckboxConfigKeys { get; } = BuildCheckboxConfigKeys();

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date)
    {
        var displayService = services.GetRequiredService<IDisplayService>();
        var galleryService = services.GetRequiredService<IGalleryService>();

        return new GalleryComponent(displayService, galleryService);
    }

    private static List<string> BuildConfigKeys()
    {
        var keys = new List<string>();
        for (int i = 1; i <= MaxGalleries; i++)
        {
            keys.Add($"gallery_id_{i}");
            keys.Add($"gallery_preference_ratio_{i}");
            keys.Add($"gallery_hide_descriptions_{i}");
        }
        return keys;
    }

    private static List<string> BuildCheckboxConfigKeys()
    {
        var keys = new List<string>();
        for (int i = 1; i <= MaxGalleries; i++)
        {
            keys.Add($"gallery_hide_descriptions_{i}");
        }
        return keys;
    }
}
