using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

public class GalleryModule : IPortalModule
{
    public string ModuleId => "gallery";
    public string? ConfigTabDisplayName => null;
    public string? ConfigPartialView => null;

    public IReadOnlyList<string> OwnedConfigKeys => ["gallery_id"];
    public IReadOnlyList<string> CheckboxConfigKeys => [];

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date)
    {
        var displayService = services.GetRequiredService<IDisplayService>();
        var galleryService = services.GetRequiredService<IGalleryService>();
        var configuration = services.GetRequiredService<IConfiguration>();

        return new GalleryComponent(displayService, galleryService, configuration);
    }
}
