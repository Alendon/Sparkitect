using Sparkitect.Modding;

namespace Sparkitect.Stateless;

// TODO: Define payload structure for function registration once SG output is finalized.
// Payload will include: functionId, wrapperType, optional ParentId (static property on wrapper).

/// <summary>
/// Base class for stateless function registries. Provides common registration logic.
/// Actual registries (PerFrameRegistry, TransitionRegistry) are shallow wrappers.
/// </summary>
public abstract class StatelessFunctionRegistryBase : IRegistryBase
{
    // TODO: Implement registration storage and lookup once payload is defined.

    public required IStatelessFunctionRegistrar Registrar { get; init; }
    
    public void Register<TStatelessFunction>(Identification id) where TStatelessFunction : IStatelessFunction
    {
        Registrar.AddFunction<TStatelessFunction>(id);
    }
    
    public void Unregister(Identification id)
    {
        // TODO: Remove function from registry
    }
}

/// <summary>
/// Registry for per-frame stateless functions.
/// </summary>
[StatelessRegistry(Identifier = "stateless:perframe")]
public sealed partial class PerFrameRegistry : StatelessFunctionRegistryBase, IRegistry
{
    public static string Identifier => "stateless:perframe";
}

/// <summary>
/// Registry for transition stateless functions.
/// </summary>
[StatelessRegistry(Identifier = "stateless:transition")]
public sealed partial class TransitionRegistry : StatelessFunctionRegistryBase, IRegistry
{
    public static string Identifier => "stateless:transition";
}
