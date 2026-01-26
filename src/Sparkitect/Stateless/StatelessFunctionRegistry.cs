using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Base class for stateless function registries. Provides common registration logic.
/// Actual registries (PerFrameRegistry, TransitionRegistry) are shallow wrappers.
/// </summary>
public abstract class StatelessFunctionRegistryBase : IRegistryBase
{
    private readonly Dictionary<Identification, Type> _wrapperTypes = new();

    public required IStatelessFunctionManager StatelessFunctionManager { get; init; }

    public void Register<TStatelessFunction>(Identification id) where TStatelessFunction : IStatelessFunction
    {
        _wrapperTypes[id] = typeof(TStatelessFunction);
        StatelessFunctionManager.AddFunction<TStatelessFunction>(id);
    }

    public void Unregister(Identification id)
    {
        _wrapperTypes.Remove(id);
    }

    internal bool TryGetWrapperType(Identification id, out Type wrapperType)
        => _wrapperTypes.TryGetValue(id, out wrapperType!);
}
