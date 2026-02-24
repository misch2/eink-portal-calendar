using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Modules.Builtin;

/// <summary>
/// Module for Telegram integration.
/// Provides only a config tab — no page-generator component.
/// </summary>
public class TelegramModule : IPortalModule
{
    public string ModuleId => "telegram";
    public string? ConfigTabDisplayName => "Telegram";
    public string? ConfigPartialView => "ConfigUI/_Telegram";

    public IReadOnlyList<string> OwnedConfigKeys =>
    [
        "telegram", "telegram_api_key", "telegram_chat_id"
    ];

    public IReadOnlyList<string> CheckboxConfigKeys => ["telegram"];

    public object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date) => null;
}
