using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Integrations;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for name day information.
/// No config tab — driven by shared locale settings.
/// </summary>
public class NameDayModule : IPortalModule
{
    public string ModuleId => "nameday";
    public string? ConfigTabDisplayName => null;
    public string? ConfigPartialView => null;

    public IReadOnlyList<string> OwnedConfigKeys => [];
    public IReadOnlyList<string> CheckboxConfigKeys => [];

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date)
    {
        var logger = services.GetRequiredService<ILogger<PageGeneratorService>>();
        var nameDayService = services.GetRequiredService<INameDayService>();
        return new NameDayComponent(logger, nameDayService);
    }
}
