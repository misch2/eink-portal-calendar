using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.PageGeneratorComponents;

namespace PortalCalendarServer.Modules.Builtin
{
    public class WakeupInfoThemeConfigModule : IPortalModule
    {
        /// <summary>
        /// Configuration items for the wakeup info bar.
        /// No config tab — icon settings are part of the Main config or individual theme config tab.
        /// </summary>
        public string ModuleId => "wakeupinfo_theme_part";
        public string? ConfigTabDisplayName => null;
        public string? ConfigPartialView => null;

        public IReadOnlyList<string> OwnedConfigKeys =>
        [
            "wakeupinfo_show_times"
        ];

        public IReadOnlyList<string> CheckboxConfigKeys => ["wakeupinfo_show_times"];

        public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date) => null;
    }
}
