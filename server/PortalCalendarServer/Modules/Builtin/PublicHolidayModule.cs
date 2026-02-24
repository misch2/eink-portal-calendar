using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Integrations;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for public holiday information.
/// No config tab — driven by shared locale settings.
/// </summary>
public class PublicHolidayModule : IPortalModule
{
    public string ModuleId => "publicholiday";
    public string? ConfigTabDisplayName => null;
    public string? ConfigPartialView => null;

    public IReadOnlyList<string> OwnedConfigKeys => [];
    public IReadOnlyList<string> CheckboxConfigKeys => [];

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date)
    {
        var logger = services.GetRequiredService<ILogger<PageGeneratorService>>();
        var publicHolidayService = services.GetRequiredService<IPublicHolidayService>();
        return new PublicHolidayComponent(logger, publicHolidayService);
    }
}
