namespace PortalCalendarServer.Modules;

/// <summary>
/// Central registry of all <see cref="IPortalModule"/> instances.
/// Register modules via <see cref="Register"/> in Program.cs before the app is built.
/// Consumed by <see cref="PortalCalendarServer.Controllers.UiController"/> and
/// <see cref="PortalCalendarServer.Services.PageGeneratorService"/>.
/// </summary>
public class ModuleRegistry
{
    private readonly List<IPortalModule> _modules = [];

    public void Register(IPortalModule module)
    {
        if (_modules.Any(m => m.ModuleId == module.ModuleId))
            throw new InvalidOperationException($"A module with id '{module.ModuleId}' is already registered.");

        _modules.Add(module);
    }

    /// <summary>All registered modules in registration order.</summary>
    public IReadOnlyList<IPortalModule> All => _modules;

    /// <summary>Modules that expose a config UI tab.</summary>
    public IReadOnlyList<IPortalModule> WithConfigTab =>
        _modules.Where(m => m.ConfigTabDisplayName != null).ToList();

    /// <summary>
    /// All config key names owned by any registered module.
    /// Used by the controller to decide which form values to persist.
    /// </summary>
    public IReadOnlySet<string> AllOwnedConfigKeys =>
        _modules.SelectMany(m => m.OwnedConfigKeys).ToHashSet();

    /// <summary>
    /// All checkbox config key names owned by any registered module.
    /// When absent from a submitted form these are explicitly cleared.
    /// </summary>
    public IReadOnlySet<string> AllCheckboxConfigKeys =>
        _modules.SelectMany(m => m.CheckboxConfigKeys).ToHashSet();
}
