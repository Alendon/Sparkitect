using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OneOf;
using OneOf.Types;
using Serilog;
using Sparkitect.CompilerGenerated;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;

namespace Sparkitect.Modding;

[Singleton<IRegistryManager>]
internal class RegistryManager : IRegistryManager
{
    internal required IModManager ModManager { get; init; }
    internal required IIdentificationManager IdentificationManager { get; init; }
    internal required IGameStateManager GameStateManager { get; init; }

    // Track which mods are processed per registry (registry category ID -> set of mod IDs)
    private readonly Dictionary<ushort, HashSet<ushort>> _processedModsByRegistry = new();


    private HashSet<string>? _lastModSet;
    private IFactoryContainer<IRegistryBase>? _registryFactory;
    
    public void AddRegistry<TRegistry>() where TRegistry : class, IRegistry
    {
        UpdateCache();
        var stringId = TRegistry.Identifier;

        if (!_registryFactory.Metadata.ContainsKey(stringId))
        {
            throw new InvalidOperationException($"No DI Metadata for {typeof(TRegistry)} found");
        }

        var numericId = IdentificationManager.RegisterCategory(stringId);
        _processedModsByRegistry.Add(numericId, []);
    }

    [MemberNotNull(nameof(_registryFactory))]
    internal void UpdateCache(ICoreContainer? coreContainer = null)
    {
        var modSet = ModManager.LoadedMods;
        if (_lastModSet?.SetEquals(modSet) is true && _registryFactory is not null) return;
        
        if (_lastModSet is null)
        {
            _lastModSet = new HashSet<string>(modSet);
        }
        else
        {
            _lastModSet.Clear();
            _lastModSet.UnionWith(modSet);
        }

        _registryFactory?.Dispose();
        var builder =
            new FactoryContainerBuilder<IRegistryBase>(coreContainer ?? GameStateManager.CurrentCoreContainer, FactoryKeyType.String);
        using var configuratorContainer = ModManager.CreateEntrypointContainer<IRegistryConfigurator>(new All());
        configuratorContainer.ProcessMany(x => x.ConfigureRegistries(builder));

        _registryFactory = builder.Build();
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
            if (IdentificationManager.TryGetModId(modId, out var modIdString))
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
        if (!IdentificationManager.TryGetCategoryId(registryId, out var registryIdentifier))
        {
            throw new InvalidOperationException($"Registry ID {registryId} not found in identification manager");
        }

        Log.Debug("Processing registry {RegistryId} ({RegistryIdentifier}) for mods: {ModIds}",
            registryId, registryIdentifier, string.Join(", ", modIdStrings));

        var registry = CreateRegistryInstance<TRegistry>();

        // Process registrations for each mod
        foreach (var modIdString in modIdStrings)
        {
            ProcessSingleModRegistration(registry, registryIdentifier, modIdString);

            // Track that this mod is processed for this registry
            if (!_processedModsByRegistry.TryGetValue(registryId, out var processedMods))
            {
                processedMods = new HashSet<ushort>();
                _processedModsByRegistry[registryId] = processedMods;
            }

            if (IdentificationManager.TryGetModId(modIdString, out var numericModId))
            {
                processedMods.Add(numericModId);
            }
        }

        Log.Debug("Completed processing registry {RegistryId} for {Count} mods", registryId, modIdStrings.Count);
    }

    public void ProcessAllMissing<TRegistry>() where TRegistry : class, IRegistry
    {
        var registryId = GetRegistryId<TRegistry>();

        // Get all loaded mods
        var allLoadedMods = ModManager.LoadedMods
            .Select(modId => IdentificationManager.TryGetModId(modId, out var numericId) ? numericId : (ushort?)null)
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

        if (!IdentificationManager.TryGetCategoryId(registryId, out var registryIdentifier))
        {
            throw new InvalidOperationException($"Registry ID {registryId} not found in identification manager");
        }

        Log.Information("Unregistering {Count} mods for registry {RegistryId}", processedMods.Count, registryId);

        var registry = CreateRegistryInstance<TRegistry>();

        // Unregister all objects from each mod
        var modsToUnregister = processedMods.ToArray(); // Copy to avoid modification during iteration
        foreach (var modId in modsToUnregister)
        {
            var objectIds = IdentificationManager.GetAllObjectIdsOfModAndCategory(modId, registryId).ToArray();

            foreach (var objectId in objectIds)
            {
                registry.Unregister(objectId);
                IdentificationManager.UnregisterObject(objectId);
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

        if (!IdentificationManager.TryGetCategoryId(identifier, out var registryId))
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
        using var registrationsContainer = ModManager.CreateEntrypointContainer<Registrations<TRegistry>>(
            OneOf<All, IEnumerable<string>>.FromT1(new[] { modId }));

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