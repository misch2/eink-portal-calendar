using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Modules.Builtin
{
    public class ThemeConfigStatusIndicatorsModule : IPortalModule
    {
        /// <summary>
        /// Configuration items for the status indicators.
        /// No config tab — icon settings are part of the Main config or individual theme config tab.
        /// </summary>
        public string ModuleId => "theme_config_status_indicators";
        public string? ConfigTabDisplayName => null;
        public string? ConfigPartialView => null;

        public IReadOnlyList<string> OwnedConfigKeys =>
        [
            "theme_config_status_wakeupinfo", "theme_config_status_battery"
        ];

        public IReadOnlyList<string> CheckboxConfigKeys => ["theme_config_status_wakeupinfo", "theme_config_status_battery"];

        public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date) => null;
    }
}
