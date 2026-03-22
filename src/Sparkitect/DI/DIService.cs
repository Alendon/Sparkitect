using System.Reflection;
using Serilog;
using Sparkitect.DI.Container;
using Sparkitect.DI.Ordering;
using Sparkitect.DI.Resolution;
using Sparkitect.GameState;

namespace Sparkitect.DI;

[StateService<IDIService, CoreModule>]
internal class DIService : IDIService
{
    private readonly Dictionary<string, Assembly> _modAssemblies = new();

    public void RegisterModAssemblies(IReadOnlyDictionary<string, Assembly> modAssemblies)
    {
        foreach (var (modId, assembly) in modAssemblies)
        {
            _modAssemblies[modId] = assembly;
            Log.Debug("Registered assembly for mod {ModId}", modId);
        }
    }

    public void UnregisterMods(IReadOnlyList<string> modIds)
    {
        foreach (var modId in modIds)
        {
            if (_modAssemblies.Remove(modId))
            {
                Log.Debug("Unregistered assembly for mod {ModId}", modId);
            }
        }
    }

    public IEntrypointContainer<T> CreateEntrypointContainer<T>(IEnumerable<string> modIds)
        where T : class, IBaseConfigurationEntrypoint
    {
        return CreateEntrypointContainer<T>(modIds, T.EntrypointAttributeType);
    }

    public IEntrypointContainer<T> CreateEntrypointContainer<T>(IEnumerable<string> modIds, Type entrypointAttribute)
        where T : class
    {
        // Phase 1: Collect ALL candidate types across ALL mods
        var allCandidateTypes = new List<Type>();

        foreach (var modId in modIds)
        {
            if (!_modAssemblies.TryGetValue(modId, out var assembly))
            {
                Log.Warning("Assembly for mod {ModId} not found in DIService", modId);
                continue;
            }

            var candidateTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(false).Any(a => a.GetType() == entrypointAttribute))
                .Where(t => typeof(T).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

            allCandidateTypes.AddRange(candidateTypes);
        }

        // Phase 2: Order all candidates deterministically
        var orderedTypes = OrderEntrypoints<T>(allCandidateTypes);

        // Phase 3: Instantiate in sorted order
        var instances = new List<T>();

        foreach (var type in orderedTypes)
        {
            // Only support parameterless constructors for configuration entrypoints
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor is null)
            {
                Log.Warning("Skipping entrypoint type {Type} without parameterless constructor", type.FullName);
                continue;
            }

            if (Activator.CreateInstance(type) is T instance)
            {
                instances.Add(instance);
            }
            else
            {
                Log.Warning("Failed to instantiate entrypoint type {Type}", type.FullName);
            }
        }

        return new EntrypointContainer<T>(instances);
    }

    public IResolutionScope BuildScope(
        ICoreContainer container,
        IResolutionProvider? provider,
        IEnumerable<string> modIds,
        IEnumerable<Type> wrapperTypes)
    {
        return BuildScope(container, provider, modIds, wrapperTypes, supplementalMetadata: null);
    }

    public IResolutionScope BuildScope(
        ICoreContainer container,
        IResolutionProvider? provider,
        IEnumerable<string> modIds,
        IEnumerable<Type> wrapperTypes,
        Dictionary<Type, List<object>>? supplementalMetadata)
    {
        var modIdList = modIds as IReadOnlyList<string> ?? modIds.ToList();
        var metadata = new Dictionary<Type, Dictionary<Type, List<object>>>();

        foreach (var wrapperType in wrapperTypes)
        {
            var attrType = typeof(ResolutionMetadataEntrypointAttribute<>).MakeGenericType(wrapperType);

            using var entrypointContainer =
                CreateEntrypointContainer<IResolutionMetadataEntrypoint>(modIdList, attrType);

            var innerDict = new Dictionary<Type, List<object>>();
            entrypointContainer.ProcessMany(entrypoint => entrypoint.ConfigureResolutionMetadata(innerDict));

            if (supplementalMetadata is not null)
            {
                foreach (var (type, entries) in supplementalMetadata)
                {
                    if (!innerDict.TryGetValue(type, out var list))
                    {
                        list = new List<object>();
                        innerDict[type] = list;
                    }
                    list.AddRange(entries);
                }
            }

            if (innerDict.Count > 0)
            {
                metadata[wrapperType] = innerDict;
            }
        }

        return new ResolutionScope(container, provider, metadata);
    }

    public IFactoryContainer<TBase> BuildFactoryContainer<TBase>(
        ICoreContainer container,
        IResolutionProvider? provider,
        IEnumerable<string> modIds,
        Type configuratorEntrypointAttribute)
        where TBase : class
    {
        var modIdList = modIds as IReadOnlyList<string> ?? modIds.ToList();
        var modIdSet = modIdList.ToHashSet() as IReadOnlySet<string>;

        // Step 1: Discover configurator entrypoints and collect factory registrations
        var factoryBuilder = new FactoryContainerBuilder<TBase>(container);

        using var configuratorContainer =
            CreateEntrypointContainer<IFactoryConfiguratorBase<TBase>>(modIdList, configuratorEntrypointAttribute);

        configuratorContainer.ProcessMany(configurator => configurator.Configure(factoryBuilder, modIdSet));

        // Step 2: Extract wrapper types from registered factories
        var wrapperTypes = factoryBuilder.GetRegisteredWrapperTypes();

        // Step 3: Build resolution scope for those wrapper types
        var scope = BuildScope(container, provider, modIdList, wrapperTypes);

        // Step 4: Build factory container (prepares all factories with scope)
        return factoryBuilder.Build(scope, skipMissing: true);
    }

    private static IReadOnlyList<Type> OrderEntrypoints<T>(IReadOnlyList<Type> allCandidateTypes)
        where T : class
    {
        if (allCandidateTypes.Count <= 1)
        {
            return allCandidateTypes;
        }

        // Build type-by-name lookup, skipping types with null FullName
        var typesByName = new Dictionary<string, Type>(allCandidateTypes.Count);

        foreach (var type in allCandidateTypes)
        {
            if (type.FullName is null)
            {
                Log.Warning("Skipping entrypoint type with null FullName: {Type}", type);
                continue;
            }

            typesByName[type.FullName] = type;
        }

        // Collect ordering constraints from attributes
        var builder = new EntrypointOrderingBuilder();

        foreach (var (fullName, type) in typesByName)
        {
            builder.SetCurrentType(fullName);

            var orderingAttributes = type.GetCustomAttributes(true).OfType<IEntrypointOrdering>();

            foreach (var ordering in orderingAttributes)
            {
                ordering.ApplyOrdering(builder);
            }
        }

        // Resolve deterministic topological order
        var resolver = new EntrypointOrderingResolver();

        IReadOnlyList<string> sortedNames;
        try
        {
            sortedNames = resolver.Resolve(typesByName.Keys, builder.Edges);
        }
        catch (InvalidOperationException ex)
        {
            Log.Error("Cycle detected in entrypoint ordering for {EntrypointType}: {Message}", typeof(T).Name, ex.Message);
            throw;
        }

        // Map sorted names back to Types
        return sortedNames
            .Where(typesByName.ContainsKey)
            .Select(name => typesByName[name])
            .ToList();
    }
}
