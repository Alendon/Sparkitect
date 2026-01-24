using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Base class for stateless function registries. Provides common registration logic.
/// Actual registries (PerFrameRegistry, TransitionRegistry) are shallow wrappers.
/// </summary>
public abstract class StatelessFunctionRegistryBase : IRegistryBase
{
    private readonly Dictionary<Identification, Type> _wrapperTypes = new();

    public required IStatelessFunctionRegistrar Registrar { get; init; }

    public void Register<TStatelessFunction>(Identification id) where TStatelessFunction : IStatelessFunction
    {
        _wrapperTypes[id] = typeof(TStatelessFunction);
        Registrar.AddFunction<TStatelessFunction>(id);
    }

    public void Unregister(Identification id)
    {
        _wrapperTypes.Remove(id);
    }

    internal bool TryGetWrapperType(Identification id, out Type wrapperType)
        => _wrapperTypes.TryGetValue(id, out wrapperType!);
}

/// <summary>
/// Registry for per-frame stateless functions.
/// </summary>
[StatelessRegistry(Identifier = "perframe_function")]
public sealed partial class PerFrameRegistry : StatelessFunctionRegistryBase, IRegistry
{
    public static string Identifier => "perframe_function";
}

/// <summary>
/// Registry for transition stateless functions.
/// </summary>
[StatelessRegistry(Identifier = "transition_function")]
public sealed partial class TransitionRegistry : StatelessFunctionRegistryBase, IRegistry
{
    public static string Identifier => "transition_function";
}
