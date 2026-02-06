using System.Reflection;
using Serilog;
using Sparkitect.DI.Container;
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
        var instances = new List<T>();

        foreach (var modId in modIds)
        {
            if (!_modAssemblies.TryGetValue(modId, out var assembly))
            {
                Log.Warning("Assembly for mod {ModId} not found in ModDIService", modId);
                continue;
            }

            // Find all types marked with the entrypoint attribute and assignable to T
            var candidateTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(false).Any(a => a.GetType() == entrypointAttribute))
                .Where(t => typeof(T).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
                .ToArray();

            // Order entrypoints deterministically (stub for now)
            var orderedTypes = OrderEntrypoints<T>(candidateTypes);

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
        }

        return new EntrypointContainer<T>(instances);
    }

    private static IEnumerable<Type> OrderEntrypoints<T>(IEnumerable<Type> types)
        where T : class, IBaseConfigurationEntrypoint
        => types;
}
