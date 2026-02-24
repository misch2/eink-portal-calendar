using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Modules;

/// <summary>
/// Represents a self-contained portal module (plugin) that can optionally provide:
/// - A page generator component accessible from Razor views via PageViewModel
/// - A configuration UI tab (partial view)
/// - A list of config setting keys stored in the database
///
/// Register modules by calling <see cref="ModuleRegistry.Register"/> in Program.cs.
/// </summary>
public interface IPortalModule
{
    /// <summary>
    /// Stable identifier used as a key in <see cref="PageViewModel.GetComponent"/>.
    /// Must be unique across all registered modules.
    /// </summary>
    string ModuleId { get; }

    // ── Config UI ────────────────────────────────────────────────────────────

    /// <summary>
    /// Human-readable name shown as the tab label in the config UI.
    /// Return <c>null</c> to skip rendering a config tab for this module.
    /// </summary>
    string? ConfigTabDisplayName { get; }

    /// <summary>
    /// URL hash / tab identifier derived from <see cref="ModuleId"/> as <c>"nav-{ModuleId}"</c>.
    /// <c>null</c> when <see cref="ConfigTabDisplayName"/> is null.
    /// </summary>
    string? ConfigTabId => ConfigTabDisplayName != null ? $"nav-{ModuleId}" : null;

    /// <summary>
    /// Path to the Razor partial view that renders this module's config section,
    /// e.g. <c>"ConfigUI/_Calendar"</c>.
    /// Return <c>null</c> when <see cref="ConfigTabDisplayName"/> is null.
    /// </summary>
    string? ConfigPartialView { get; }

    /// <summary>
    /// All config key names that belong to this module (plain text fields, dropdowns, …).
    /// The controller will persist every key listed here that is present in the submitted form.
    /// </summary>
    IReadOnlyList<string> OwnedConfigKeys { get; }

    /// <summary>
    /// Subset of <see cref="OwnedConfigKeys"/> that are boolean toggles (checkboxes).
    /// When a checkbox is absent from the submitted form its value is explicitly set to ""
    /// (i.e. treated as unchecked / false).
    /// </summary>
    IReadOnlyList<string> CheckboxConfigKeys { get; }

    // ── Page generator component ─────────────────────────────────────────────

    /// <summary>
    /// Factory that creates the page-generator component for this module.
    /// Return <c>null</c> if this module has no page-generator component.
    /// The component is wrapped in a <see cref="Lazy{T}"/> so it is only
    /// constructed when the view actually accesses it.
    /// </summary>
    object? CreatePageGeneratorComponent(IServiceProvider services, Display display, DateTime date);
}
