using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

/// <summary>
/// Registry for per-frame stateless functions.
/// </summary>
[StatelessRegistry(Identifier = "perframe_function", External = true)]
public sealed partial class PerFrameRegistry : StatelessFunctionRegistryBase, IRegistry
{
    public static string Identifier => "perframe_function";
}

/// <summary>
/// Registry for transition stateless functions.
/// </summary>
[StatelessRegistry(Identifier = "transition_function", External = true)]
public sealed partial class TransitionRegistry : StatelessFunctionRegistryBase, IRegistry
{
    public static string Identifier => "transition_function";
}
