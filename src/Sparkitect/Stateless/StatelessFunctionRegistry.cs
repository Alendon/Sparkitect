using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Base class for stateless function registries. Provides common registration logic.
/// Actual registries (PerFrameRegistry, TransitionRegistry) are shallow wrappers.
/// </summary>
[PublicAPI]
public abstract class StatelessFunctionRegistryBase : IRegistryBase
{
    private readonly Dictionary<Identification, Type> _wrapperTypes = new();

    /// <summary>The manager that receives registered wrapper types; injected by the container.</summary>
    public required IStatelessFunctionManager StatelessFunctionManager { get; init; }

    /// <summary>Registers a generated wrapper type under the given identification and forwards it to the manager.</summary>
    public virtual void Register<TStatelessFunction>(Identification id) where TStatelessFunction : IStatelessFunction
    {
        _wrapperTypes[id] = typeof(TStatelessFunction);
        StatelessFunctionManager.AddFunction<TStatelessFunction>(id);
    }

    /// <summary>Removes a previously registered function by identification.</summary>
    public void Unregister(Identification id)
    {
        _wrapperTypes.Remove(id);
    }

    internal bool TryGetWrapperType(Identification id, out Type wrapperType)
        => _wrapperTypes.TryGetValue(id, out wrapperType!);
}
