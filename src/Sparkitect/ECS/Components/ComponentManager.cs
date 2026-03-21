using System.Runtime.CompilerServices;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Components;

/// <summary>
/// Stores component size metadata per <see cref="Identification"/>.
/// Backing store for <see cref="IComponentManager"/>.
/// </summary>
[StateService<IComponentManager, EcsModule>]
public class ComponentManager : IComponentManager
{
    private readonly Dictionary<Identification, int> _components = new();

    /// <inheritdoc/>
    public void Register<T>(Identification id) where T : unmanaged
    {
        if (!_components.TryAdd(id, Unsafe.SizeOf<T>()))
            throw new ArgumentException($"Component '{id}' is already registered.", nameof(id));
    }

    /// <inheritdoc/>
    public int GetSize(Identification id)
    {
        if (!_components.TryGetValue(id, out var size))
            throw new KeyNotFoundException($"Component '{id}' is not registered.");

        return size;
    }

    /// <inheritdoc/>
    public bool IsRegistered(Identification id)
    {
        return _components.ContainsKey(id);
    }
}
