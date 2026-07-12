using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Serilog;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;
using Sparkitect.GameState;
using Sparkitect.Utils.DU;

namespace Sparkitect.Modding;

[StateService<IRegistryManager, CoreModule>]
internal class RegistryManager : IRegistryManager, IRegistryLifecycleManager
{
    internal required IModManager ModManager { get; init; }
    internal required IIdentificationManager IdentificationManager { get; init; }
    internal required IGameStateManager GameStateManager { get; init; }
    internal required IDIService DIService { get; init; }
    internal required IResourceManager ResourceManager { get; init; }
    private readonly RegistryState _registryState = new();

    // Registry identifier -> set of mod IDs processed for it. Presence of a key = the registry is added.
    private readonly Dictionary<string, HashSet<string>> _processedModsByRegistry = new();

    // Registry identifier -> owning module, recorded at add time so removal never re-resolves.
    private readonly Dictionary<string, Identification> _moduleByRegistry = new();

    private HashSet<string>? _lastModSet;
    private IFactoryContainer<string, IRegistryBase>? _registryFactory;
    private ICoreContainer? _lastCoreContainer;

    private static readonly MethodInfo ProcessRegistryForModsMethod =
        typeof(RegistryManager).GetMethod(nameof(ProcessRegistryForMods),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo ReverseRegistrationsForModsMethod =
        typeof(RegistryManager).GetMethod(nameof(ReverseRegistrationsForMods),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private GsmTransitionDirection CurrentDirection =>
        ((IGameStateTransitionSignal)GameStateManager).TransitionDirection;

    // ── Module-facing surface ──

    public void ProcessRegistry<TRegistry, TModule>()
        where TRegistry : class, IRegistry<TModule>
        where TModule : IHasIdentification, IStateModule
    {
        switch (CurrentDirection)
        {
            case GsmTransitionDirection.Enter:
                PopulateMissingMods<TRegistry>();
                break;
            case GsmTransitionDirection.Exit:
                ReverseAllRegistrations<TRegistry>();
                break;
            default:
                throw new InvalidOperationException(
                    $"ProcessRegistry<{typeof(TRegistry).Name}, {typeof(TModule).Name}>() must run inside a state " +
                    "transition; the game-state manager reports no active transition direction.");
        }
    }

    public IEnumerable<string> GetActiveRegistries() => _processedModsByRegistry.Keys;

    public IEnumerable<string> GetProcessedMods<TRegistry>() where TRegistry : class, IRegistry
    {
        var registryIdentifier = TRegistry.Identifier;
        return _processedModsByRegistry.TryGetValue(registryIdentifier, out var processedMods)
            ? processedMods
            : Enumerable.Empty<string>();
    }

    public bool IsRegistryActive<TRegistry>() where TRegistry : class, IRegistry =>
        _processedModsByRegistry.ContainsKey(TRegistry.Identifier);

    // ── Automatic instance lifecycle (GSM composite hooks) ──

    public void AddModuleRegistries(Identification moduleId, ICoreContainer container)
    {
        UpdateCache(container);
        foreach (var (identifier, instance) in _registryFactory.ResolveAll())
        {
            if (!ReadOwningModule(instance.GetType()).Equals(moduleId)) continue;
            if (_processedModsByRegistry.ContainsKey(identifier)) continue;

            AddRegistryTracking(identifier, instance.GetType());
            _moduleByRegistry[identifier] = moduleId;
        }
    }

    public void RemoveModuleRegistries(Identification moduleId)
    {
        var owned = _moduleByRegistry
            .Where(kv => kv.Value.Equals(moduleId))
            .Select(kv => kv.Key)
            .ToList();

        foreach (var identifier in owned)
            RemoveRegistryTracking(identifier);
    }

    public void ProcessModuleRegistriesForMods(Identification moduleId, IReadOnlyList<string> modIds,
        ICoreContainer container)
    {
        UpdateCache(container);
        foreach (var (_, instance) in _registryFactory.ResolveAll())
        {
            var type = instance.GetType();
            if (!ReadOwningModule(type).Equals(moduleId)) continue;
            InvokeProcessForMods(type, modIds);
        }
    }

    public void UnprocessModuleRegistriesForMods(Identification moduleId, IReadOnlyList<string> modIds,
        ICoreContainer container)
    {
        UpdateCache(container);

        // Snapshot ownership before any reversal: unregistering the module registry clears the generated
        // Identification statics that ReadOwningModule reads. Then reverse in the opposite of resolve
        // order — the module/state registries resolve first and carry the ids later entries depend on,
        // so they must be the last to go down.
        var owned = new List<Type>();
        foreach (var (_, instance) in _registryFactory.ResolveAll())
        {
            var type = instance.GetType();
            if (ReadOwningModule(type).Equals(moduleId)) owned.Add(type);
        }

        for (var i = owned.Count - 1; i >= 0; i--)
            InvokeUnprocessForMods(owned[i], modIds);
    }

    public void BootstrapRootRegistries(IReadOnlyList<string> modIds, ICoreContainer container)
    {
        // At root entry only CoreModule's registries resolve; their owning module is not registered yet, so
        // add every currently-resolvable registry without owner filtering, then process it for the root mods
        // (which registers the modules/states the finalize pass validates), then record the now-valid owner.
        UpdateCache(container);
        var registries = _registryFactory.ResolveAll();

        _registryState.EnterPopulating();
        try
        {
            foreach (var (identifier, instance) in registries)
                if (!_processedModsByRegistry.ContainsKey(identifier))
                    AddRegistryTracking(identifier, instance.GetType());

            foreach (var (_, instance) in registries)
                InvokeProcessForMods(instance.GetType(), modIds);

            foreach (var (identifier, instance) in registries)
                _moduleByRegistry[identifier] = ReadOwningModule(instance.GetType());
        }
        finally
        {
            _registryState.ReturnToIdle();
        }
    }

    private void AddRegistryTracking(string identifier, Type registryType)
    {
        IdentificationManager.RegisterCategory(identifier);
        _processedModsByRegistry[identifier] = new HashSet<string>();

        if (ReadResourceFolder(registryType) is { } folder)
            ResourceManager.RegisterResourceFolder(identifier, folder);
    }

    private void RemoveRegistryTracking(string identifier)
    {
        // Registrations this registry created are reversed on the module-driven teardown path (ProcessRegistry
        // at [OnFrameExit]) before this runs; native/GPU resources are never touched here, so instance removal
        // carries no device-idle ordering concern. The category itself must also unregister here so a later
        // process-up re-registers cleanly instead of colliding with the still-held category id.
        _processedModsByRegistry.Remove(identifier);
        _moduleByRegistry.Remove(identifier);

        if (IdentificationManager.GetCategoryId(identifier) is not Result<ushort, ResolveError>.Ok(var categoryId))
            return;

        if (!IdentificationManager.UnregisterCategory(categoryId))
            throw new InvalidOperationException(
                $"Registry '{identifier}' still has objects registered under its category after teardown; " +
                "entries must be fully reversed before the category can be removed.");
    }

    private static Identification ReadOwningModule(Type registryType)
    {
        var property = registryType.GetProperty("OwningModule", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"Registry '{registryType.Name}' exposes no static OwningModule; it must implement IRegistry<TModule>.");
        return (Identification)property.GetValue(null)!;
    }

    private static string? ReadResourceFolder(Type registryType) =>
        registryType.GetProperty("ResourceFolder", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string;

    private void InvokeProcessForMods(Type registryType, IReadOnlyList<string> modIds)
    {
        try
        {
            ProcessRegistryForModsMethod.MakeGenericMethod(registryType).Invoke(this, new object[] { modIds });
        }
        catch (TargetInvocationException tie) when (tie.InnerException is not null)
        {
            throw tie.InnerException;
        }
    }

    private void InvokeUnprocessForMods(Type registryType, IReadOnlyList<string> modIds)
    {
        try
        {
            ReverseRegistrationsForModsMethod.MakeGenericMethod(registryType).Invoke(this, new object[] { modIds });
        }
        catch (TargetInvocationException tie) when (tie.InnerException is not null)
        {
            throw tie.InnerException;
        }
    }

    // ── Generation: populate / teardown ──

    [MemberNotNull(nameof(_registryFactory))]
    internal void UpdateCache(ICoreContainer? coreContainer = null)
    {
        var modIds = ModManager.LoadedMods.Select(m => m.Id).ToList();
        var effectiveContainer = coreContainer ?? GameStateManager.CurrentCoreContainer;

        if (_lastModSet?.SetEquals(modIds) is true && _registryFactory is not null
            && effectiveContainer.Equals(_lastCoreContainer)) return;

        if (_lastModSet is null)
            _lastModSet = new HashSet<string>(modIds);
        else
        {
            _lastModSet.Clear();
            _lastModSet.UnionWith(modIds);
        }

        _registryFactory?.Dispose();

        var provider = new FacadeResolutionProvider();
        _registryFactory = DIService.BuildFactoryContainer<string, IRegistryBase>(
            effectiveContainer, provider, modIds,
            typeof(RegistryConfiguratorAttribute), true);
        _lastCoreContainer = effectiveContainer;
    }

    private TRegistry CreateRegistryInstance<TRegistry>() where TRegistry : class, IRegistry
    {
        UpdateCache();
        _registryFactory.TryResolve(TRegistry.Identifier, out var instance);
        if (instance is not TRegistry typedInstance)
            throw new InvalidOperationException(
                $"Resolved Instance {instance} is not of expected type {typeof(TRegistry)}");

        return typedInstance;
    }

    /// <summary>
    /// Populates a registry from a specific set of mods. Retained as the internal path the GSM bootstrap
    /// (root entry, mod-change) drives; module authors use the direction-detecting <see cref="ProcessRegistry{TRegistry,TModule}"/>.
    /// </summary>
    private void ProcessRegistryForMods<TRegistry>(IReadOnlyList<string> modIds) where TRegistry : class, IRegistry
    {
        var registryIdentifier = TRegistry.Identifier;

        if (modIds.Count == 0)
        {
            Log.Debug("ProcessRegistry called with no mods for registry {RegistryIdentifier}", registryIdentifier);
            return;
        }

        _registryState.EnterPopulating();
        try
        {
            Log.Debug("Processing registry {RegistryIdentifier} for mods: {ModIds}",
                registryIdentifier, string.Join(", ", modIds));

            var registry = CreateRegistryInstance<TRegistry>();

            foreach (var modId in modIds)
            {
                _registryState.MutationRequest(new RegistryOperation.Allocate(modId, registryIdentifier), default);
                ProcessSingleModRegistration(registry, registryIdentifier, modId);

                if (!_processedModsByRegistry.TryGetValue(registryIdentifier, out var processedMods))
                {
                    processedMods = new HashSet<string>();
                    _processedModsByRegistry[registryIdentifier] = processedMods;
                }

                processedMods.Add(modId);
            }

            Log.Debug("Completed processing registry {RegistryIdentifier} for {Count} mods", registryIdentifier, modIds.Count);
        }
        finally
        {
            _registryState.ReturnToIdle();
        }
    }

    private void PopulateMissingMods<TRegistry>() where TRegistry : class, IRegistry
    {
        var registryIdentifier = TRegistry.Identifier;

        var allLoadedModIds = ModManager.LoadedMods.Select(m => m.Id).ToHashSet();

        var processedMods = _processedModsByRegistry.TryGetValue(registryIdentifier, out var processed)
            ? processed
            : new HashSet<string>();

        var missingMods = allLoadedModIds.Except(processedMods).ToList();

        if (missingMods.Count == 0)
        {
            Log.Debug("No missing mods to process for registry {RegistryIdentifier}", registryIdentifier);
            return;
        }

        Log.Information("Processing {Count} missing mods for registry {RegistryIdentifier}", missingMods.Count, registryIdentifier);
        ProcessRegistryForMods<TRegistry>(missingMods);
    }

    private void ReverseAllRegistrations<TRegistry>() where TRegistry : class, IRegistry
    {
        var registryIdentifier = TRegistry.Identifier;

        if (!_processedModsByRegistry.TryGetValue(registryIdentifier, out var processedMods) || processedMods.Count == 0)
        {
            Log.Debug("No mods to unregister for registry {RegistryIdentifier}", registryIdentifier);
            return;
        }

        ReverseRegistrationsForMods<TRegistry>(processedMods.ToArray());
    }

    private void ReverseRegistrationsForMods<TRegistry>(IReadOnlyList<string> modIds) where TRegistry : class, IRegistry
    {
        var registryIdentifier = TRegistry.Identifier;

        if (!_processedModsByRegistry.TryGetValue(registryIdentifier, out var processedMods))
        {
            return;
        }

        var modsToUnregister = modIds.Where(processedMods.Contains).ToArray();
        if (modsToUnregister.Length == 0)
        {
            return;
        }

        Log.Information("Unregistering {Count} mods for registry {RegistryIdentifier}", modsToUnregister.Length, registryIdentifier);

        _registryState.EnterTearingDown();
        try
        {
            var registry = CreateRegistryInstance<TRegistry>();

            foreach (var modId in modsToUnregister)
            {
                ProcessSingleModUnregistration(registry, modId);
                processedMods.Remove(modId);
            }

            Log.Debug("Completed unregistering mods for registry {RegistryIdentifier}", registryIdentifier);
        }
        finally
        {
            _registryState.ReturnToIdle();
        }
    }

    private void ProcessSingleModUnregistration<TRegistry>(TRegistry registry, string modId)
        where TRegistry : class, IRegistry
    {
        using var registrationsContainer = DIService.CreateEntrypointContainer<Registrations<TRegistry>>(
            new[] { modId });

        var registrations = registrationsContainer.ResolveMany();

        var scope = DIService.BuildScope(
            GameStateManager.CurrentCoreContainer,
            new FacadeResolutionProvider(),
            new[] { modId },
            Array.Empty<Type>());

        foreach (var registration in registrations)
        {
            registration.Initialize(scope);
            registration.ProcessUnregistrations(registry);
        }
    }

    private void ProcessSingleModRegistration<TRegistry>(TRegistry registry, string registryIdentifier, string modId)
        where TRegistry : class, IRegistry
    {
        using var registrationsContainer = DIService.CreateEntrypointContainer<Registrations<TRegistry>>(
            new[] { modId });

        var registrations = registrationsContainer.ResolveMany();

        var scope = DIService.BuildScope(
            GameStateManager.CurrentCoreContainer,
            new FacadeResolutionProvider(),
            new[] { modId },
            Array.Empty<Type>());

        foreach (var registration in registrations)
        {
            registration.Initialize(scope);
            registration.ProcessRegistrations(registry);
        }
    }
}
