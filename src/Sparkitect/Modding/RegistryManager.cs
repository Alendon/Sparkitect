using System.Diagnostics.CodeAnalysis;
using Serilog;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;
using Sparkitect.GameState;

namespace Sparkitect.Modding;

[StateService<IRegistryManager, CoreModule>]
internal class RegistryManager : IRegistryManager
{
    internal required IModManager ModManager { get; init; }
    internal required IIdentificationManager IdentificationManager { get; init; }
    internal required IGameStateManager GameStateManager { get; init; }
    internal required IDIService DIService { get; init; }
    internal required IResourceManager ResourceManager { get; init; }

    // Track which mods are processed per registry (registry identifier -> set of mod IDs)
    private readonly Dictionary<string, HashSet<string>> _processedModsByRegistry = new();

    private bool _isMutationExpected;

    public bool IsMutationExpected => _isMutationExpected;

    private HashSet<string>? _lastModSet;
    private IFactoryContainer<IRegistryBase>? _registryFactory;
    
    public void AddRegistry<TRegistry>() where TRegistry : class, IRegistry
    {
        UpdateCache();
        var identifier = TRegistry.Identifier;

        if (!_registryFactory.Metadata.ContainsKey(identifier))
        {
            throw new InvalidOperationException($"No DI Metadata for {typeof(TRegistry)} found");
        }

        IdentificationManager.RegisterCategory(identifier);
        _processedModsByRegistry.Add(identifier, []);

        if (TRegistry.ResourceFolder is { } folder)
        {
            ResourceManager.RegisterResourceFolder(identifier, folder);
        }
    }

    private ICoreContainer? _lastCoreContainer;
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
        _registryFactory = DIService.BuildFactoryContainer<IRegistryBase>(
            effectiveContainer, provider, modIds,
            typeof(RegistryConfiguratorAttribute));
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

    public void ProcessRegistry<TRegistry>(IReadOnlyList<string> modIds) where TRegistry : class, IRegistry
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

            // Process registrations for each mod
            foreach (var modId in modIds)
            {
                ProcessSingleModRegistration(registry, registryIdentifier, modId);

                // Track that this mod is processed for this registry
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

    public void ProcessAllMissing<TRegistry>() where TRegistry : class, IRegistry
    {
        var registryIdentifier = TRegistry.Identifier;

        // Get all loaded mod IDs (runtime identification is ID-based)
        var allLoadedModIds = ModManager.LoadedMods.Select(m => m.Id).ToHashSet();

        // Get already processed mods for this registry
        var processedMods = _processedModsByRegistry.TryGetValue(registryIdentifier, out var processed)
            ? processed
            : new HashSet<string>();

        // Find missing mods
        var missingMods = allLoadedModIds.Except(processedMods).ToList();

        if (missingMods.Count == 0)
        {
            Log.Debug("No missing mods to process for registry {RegistryIdentifier}", registryIdentifier);
            return;
        }

        Log.Information("Processing {Count} missing mods for registry {RegistryIdentifier}", missingMods.Count, registryIdentifier);
        ProcessRegistry<TRegistry>(missingMods);
    }

    public void UnregisterAllRemaining<TRegistry>() where TRegistry : class, IRegistry
    {
        var registryIdentifier = TRegistry.Identifier;

        if (!_processedModsByRegistry.TryGetValue(registryIdentifier, out var processedMods) || processedMods.Count == 0)
        {
            Log.Debug("No mods to unregister for registry {RegistryIdentifier}", registryIdentifier);
            return;
        }

        if (!IdentificationManager.TryGetCategoryId(registryIdentifier, out var registryCategoryId))
        {
            throw new InvalidOperationException($"Registry identifier '{registryIdentifier}' not found in identification manager");
        }

        Log.Information("Unregistering {Count} mods for registry {RegistryIdentifier}", processedMods.Count, registryIdentifier);

        var registry = CreateRegistryInstance<TRegistry>();

        // Unregister all objects from each mod
        var modsToUnregister = processedMods.ToArray(); // Copy to avoid modification during iteration
        foreach (var modId in modsToUnregister)
        {
            if (!IdentificationManager.TryGetModId(modId, out var numericModId))
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

    public IEnumerable<string> GetActiveRegistries()
    {
        return _processedModsByRegistry.Keys;
    }

    public IEnumerable<string> GetProcessedMods<TRegistry>() where TRegistry : class, IRegistry
    {
        var registryIdentifier = TRegistry.Identifier;
        return _processedModsByRegistry.TryGetValue(registryIdentifier, out var processedMods)
            ? processedMods
            : Enumerable.Empty<string>();
    }

    public bool IsRegistryActive<TRegistry>() where TRegistry : class, IRegistry
    {
        var registryIdentifier = TRegistry.Identifier;
        return _processedModsByRegistry.ContainsKey(registryIdentifier);
    }

    private void ProcessSingleModRegistration<TRegistry>(TRegistry registry, string registryIdentifier, string modId)
        where TRegistry : class, IRegistry
    {
        // Query registrations for this specific registry type
        // The generic attribute RegistrationsEntrypointAttribute<TRegistry> provides automatic filtering
        using var registrationsContainer = DIService.CreateEntrypointContainer<Registrations<TRegistry>>(
            new[] { modId });

        var registrations = registrationsContainer.ResolveMany();

        foreach (var registration in registrations)
        {
            // Initialize with current container
            registration.Initialize(GameStateManager.CurrentCoreContainer);

            // Process registrations with typed registry
            registration.ProcessRegistrations(registry);
        }
    }
}