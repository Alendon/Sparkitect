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

    // Registry identifier -> set of mod IDs processed for it. Presence of a key = the registry is added.
    private readonly Dictionary<string, HashSet<string>> _processedModsByRegistry = new();

    // Registry identifier -> owning module, recorded at add time so removal never re-resolves.
    private readonly Dictionary<string, Identification> _moduleByRegistry = new();

    private bool _isMutationExpected;

    public bool IsMutationExpected => _isMutationExpected;

    private HashSet<string>? _lastModSet;
    private IFactoryContainer<string, IRegistryBase>? _registryFactory;
    private ICoreContainer? _lastCoreContainer;

    private static readonly MethodInfo ProcessRegistryForModsMethod =
        typeof(RegistryManager).GetMethod(nameof(ProcessRegistryForMods),
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

    public void BootstrapRootRegistries(IReadOnlyList<string> modIds, ICoreContainer container)
    {
        // At root entry only CoreModule's registries resolve; their owning module is not registered yet, so
        // add every currently-resolvable registry without owner filtering, then process it for the root mods
        // (which registers the modules/states the finalize pass validates), then record the now-valid owner.
        UpdateCache(container);
        var registries = _registryFactory.ResolveAll();

        foreach (var (identifier, instance) in registries)
            if (!_processedModsByRegistry.ContainsKey(identifier))
                AddRegistryTracking(identifier, instance.GetType());

        foreach (var (_, instance) in registries)
            InvokeProcessForMods(instance.GetType(), modIds);

        foreach (var (identifier, instance) in registries)
            _moduleByRegistry[identifier] = ReadOwningModule(instance.GetType());
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
        // Bookkeeping only: drop the tracking entry. Registrations this registry created are reversed on the
        // module-driven teardown path (ProcessRegistry at [OnFrameExit]); native/GPU resources are never
        // touched here, so instance removal carries no device-idle ordering concern.
        _processedModsByRegistry.Remove(identifier);
        _moduleByRegistry.Remove(identifier);
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

        _isMutationExpected = true;
        try
        {
            Log.Debug("Processing registry {RegistryIdentifier} for mods: {ModIds}",
                registryIdentifier, string.Join(", ", modIds));

            var registry = CreateRegistryInstance<TRegistry>();

            foreach (var modId in modIds)
            {
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
            _isMutationExpected = false;
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

        if (IdentificationManager.GetCategoryId(registryIdentifier) is not Result<ushort, ResolveError>.Ok(var registryCategoryId))
        {
            throw new InvalidOperationException($"Registry identifier '{registryIdentifier}' not found in identification manager");
        }

        Log.Information("Unregistering {Count} mods for registry {RegistryIdentifier}", processedMods.Count, registryIdentifier);

        var registry = CreateRegistryInstance<TRegistry>();

        var modsToUnregister = processedMods.ToArray();
        foreach (var modId in modsToUnregister)
        {
            if (IdentificationManager.GetModId(modId) is not Result<ushort, ResolveError>.Ok(var numericModId))
            {
                Log.Warning("Mod ID {ModId} not found in identification manager during unregister", modId);
                continue;
            }

            var objectIds = IdentificationManager.GetAllObjectIdsOfModAndCategory(numericModId, registryCategoryId).ToArray();

            foreach (var objectId in objectIds)
            {
                registry.Unregister(objectId);
                IdentificationManager.UnregisterObject(objectId);
            }

            processedMods.Remove(modId);
        }

        Log.Debug("Completed unregistering mods for registry {RegistryIdentifier}", registryIdentifier);
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
