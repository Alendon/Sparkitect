using System.Reflection;
using Serilog;
using Sparkitect.DI.Container;
using Sparkitect.DI.Ordering;
using Sparkitect.GameState;

namespace Sparkitect.DI;

[StateService<IModDIService, CoreModule>]
internal class ModDIService : IModDIService
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
        var entrypointAttribute = T.EntrypointAttributeType;

        // Phase 1: Collect ALL candidate types across ALL mods
        var allCandidateTypes = new List<Type>();

        foreach (var modId in modIds)
        {
            if (!_modAssemblies.TryGetValue(modId, out var assembly))
            {
                Log.Warning("Assembly for mod {ModId} not found in ModDIService", modId);
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

    private static IReadOnlyList<Type> OrderEntrypoints<T>(IReadOnlyList<Type> allCandidateTypes)
        where T : class, IBaseConfigurationEntrypoint
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
