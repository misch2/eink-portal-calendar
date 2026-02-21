namespace PortalCalendarServer.Models.Options;

public class UiOptions
{
    public const string SectionName = "UI";

    /// <summary>
    /// Optional CSS file to include in all UI pages, relative to wwwroot.
    /// Used to visually distinguish environments (e.g. "css/environment/development.css").
    /// Leave empty or omit for production.
    /// </summary>
    public string EnvironmentCssFile { get; set; } = string.Empty;
}
