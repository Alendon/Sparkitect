using JetBrains.Annotations;
using OneOf;
using OneOf.Types;
using Serilog;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;

namespace Sparkitect.Modding;

[Singleton<IRegistryManager>]
internal class RegistryManager : IRegistryManager
{
    internal required IModManager _modManager { get; init; }
    internal required IIdentificationManager _identificationManager { get; init; }
    internal required IGameStateManager _gameStateManager { get; init; }

    // Track which mods are processed per registry (registry category ID -> set of mod IDs)
    private readonly Dictionary<ushort, HashSet<ushort>> _processedModsByRegistry = new();

    // Cache for registry configurator output (rebuild when mod set changes)
    private IFactoryContainer<IRegistry>? _registryContainerCache;
    private HashSet<string>? _cachedModSet;

    public void ProcessRegistry<TRegistry>(params Span<ushort> modIds) where TRegistry : class, IRegistry
    {
        var registryId = GetRegistryId<TRegistry>();

        if (modIds.Length == 0)
        {
            Log.Debug("ProcessRegistry called with no mods for registry {RegistryId}", registryId);
            return;
        }

        // Convert numeric mod IDs to string IDs for entrypoint query
        var modIdStrings = new List<string>();
        foreach (var modId in modIds)
        {
            if (_identificationManager.TryGetModId(modId, out var modIdString))
            {
                modIdStrings.Add(modIdString);
            }
            else
            {
                Log.Warning("Mod ID {ModId} not found in identification manager", modId);
            }
        }

        if (modIdStrings.Count == 0)
        {
            Log.Warning("No valid mod IDs found for registry {RegistryId}", registryId);
            return;
        }

        // Get registry identifier string
        if (!_identificationManager.TryGetCategoryId(registryId, out var registryIdentifier))
        {
            throw new InvalidOperationException($"Registry ID {registryId} not found in identification manager");
        }

        Log.Debug("Processing registry {RegistryId} ({RegistryIdentifier}) for mods: {ModIds}",
            registryId, registryIdentifier, string.Join(", ", modIdStrings));

        // Get or build registry container
        var registryContainer = GetOrBuildRegistryContainer();

        // Resolve the specific registry by identifier
        if (!registryContainer.TryResolve(registryIdentifier, out var registry))
        {
            throw new InvalidOperationException($"Registry '{registryIdentifier}' not found in container");
        }

        // Process registrations for each mod
        foreach (var modIdString in modIdStrings)
        {
            ProcessSingleModRegistration<TRegistry>(registry, registryIdentifier, modIdString);

            // Track that this mod is processed for this registry
            if (!_processedModsByRegistry.TryGetValue(registryId, out var processedMods))
            {
                processedMods = new HashSet<ushort>();
                _processedModsByRegistry[registryId] = processedMods;
            }

            if (_identificationManager.TryGetModId(modIdString, out var numericModId))
            {
                processedMods.Add(numericModId);
            }
        }

        Log.Debug("Completed processing registry {RegistryId} for {Count} mods", registryId, modIdStrings.Count);
    }

    public void ProcessRegistry(ushort registryId, params Span<ushort> modIds)
    {
        throw new NotSupportedException(
            "Non-generic ProcessRegistry not supported. Use generic ProcessRegistry<TRegistry>() instead.");
    }

    public void ProcessAllMissing<TRegistry>() where TRegistry : class, IRegistry
    {
        var registryId = GetRegistryId<TRegistry>();

        // Get all loaded mods
        var allLoadedMods = _modManager.LoadedMods
            .Select(modId => _identificationManager.TryGetModId(modId, out var numericId) ? numericId : (ushort?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        // Get already processed mods for this registry
        var processedMods = _processedModsByRegistry.TryGetValue(registryId, out var processed)
            ? processed
            : new HashSet<ushort>();

        // Find missing mods
        var missingMods = allLoadedMods.Except(processedMods).ToArray();

        if (missingMods.Length == 0)
        {
            Log.Debug("No missing mods to process for registry {RegistryId}", registryId);
            return;
        }

        Log.Information("Processing {Count} missing mods for registry {RegistryId}", missingMods.Length, registryId);
        ProcessRegistry<TRegistry>(missingMods);
    }

    public void UnregisterAllRemaining<TRegistry>() where TRegistry : class, IRegistry
    {
        var registryId = GetRegistryId<TRegistry>();

        if (!_processedModsByRegistry.TryGetValue(registryId, out var processedMods) || processedMods.Count == 0)
        {
            Log.Debug("No mods to unregister for registry {RegistryId}", registryId);
            return;
        }

        if (!_identificationManager.TryGetCategoryId(registryId, out var registryIdentifier))
        {
            throw new InvalidOperationException($"Registry ID {registryId} not found in identification manager");
        }

        Log.Information("Unregistering {Count} mods for registry {RegistryId}", processedMods.Count, registryId);

        // Get registry container
        var registryContainer = GetOrBuildRegistryContainer();

        if (!registryContainer.TryResolve(registryIdentifier, out var registry))
        {
            throw new InvalidOperationException($"Registry '{registryIdentifier}' not found in container");
        }

        // Unregister all objects from each mod
        var modsToUnregister = processedMods.ToArray(); // Copy to avoid modification during iteration
        foreach (var modId in modsToUnregister)
        {
            var objectIds = _identificationManager.GetAllObjectIdsOfModAndCategory(modId, registryId).ToArray();

            foreach (var objectId in objectIds)
            {
                registry.Unregister(objectId);
                _identificationManager.UnregisterObject(objectId);
            }

            processedMods.Remove(modId);
        }

        Log.Debug("Completed unregistering mods for registry {RegistryId}", registryId);
    }

    private ushort GetRegistryId<TRegistry>() where TRegistry : class, IRegistry
    {
        // Extract registry identifier from [Registry(Identifier = "...")] attribute
        var registryType = typeof(TRegistry);
        var registryAttribute = registryType.GetCustomAttributes(typeof(Attribute), false)
            .FirstOrDefault(attr => attr.GetType().Name == "RegistryAttribute");

        if (registryAttribute == null)
        {
            throw new InvalidOperationException($"Registry type {registryType.Name} does not have [Registry] attribute");
        }

        var identifierProperty = registryAttribute.GetType().GetProperty("Identifier");
        if (identifierProperty == null || identifierProperty.GetValue(registryAttribute) is not string identifier)
        {
            throw new InvalidOperationException($"Registry type {registryType.Name} [Registry] attribute missing Identifier property");
        }

        if (!_identificationManager.TryGetCategoryId(identifier, out var registryId))
        {
            throw new InvalidOperationException($"Registry identifier '{identifier}' not found in identification manager. Category must be registered first.");
        }

        return registryId;
    }

    private void ProcessSingleModRegistration<TRegistry>(TRegistry registry, string registryIdentifier, string modId)
        where TRegistry : class, IRegistry
    {
        // Query registrations for this specific registry type
        // The generic attribute RegistrationsEntrypointAttribute<TRegistry> provides automatic filtering
        using var registrationsContainer = _modManager.CreateEntrypointContainer<Registrations<TRegistry>>(
            OneOf<All, IEnumerable<string>>.FromT1(new[] { modId }));

        var registrations = registrationsContainer.ResolveMany();

        foreach (var registration in registrations)
        {
            // Initialize with current container
            registration.Initialize(_gameStateManager.CurrentCoreContainer);

            // Process registrations with typed registry
            registration.ProcessRegistrations(registry);
        }
    }

    [MustDisposeResource]
    private IFactoryContainer<IRegistry> GetOrBuildRegistryContainer()
    {
        // Check if cache is valid (mod set unchanged)
        var currentModSet = _modManager.LoadedMods.ToHashSet();

        if (_registryContainerCache != null && _cachedModSet != null && _cachedModSet.SetEquals(currentModSet))
        {
            return _registryContainerCache;
        }

        // Rebuild cache
        Log.Debug("Rebuilding registry container cache");

        _registryContainerCache?.Dispose();

        using var configurators = _modManager.CreateEntrypointContainer<IRegistryConfigurator>(new All());

        var containerBuilder = new FactoryContainerBuilder<IRegistry>(
            _gameStateManager.CurrentCoreContainer,
            FactoryKeyType.String);

        configurators.ProcessMany(c => c.ConfigureRegistries(containerBuilder));

        _registryContainerCache = containerBuilder.Build();
        _cachedModSet = currentModSet;

        return _registryContainerCache;
    }
}