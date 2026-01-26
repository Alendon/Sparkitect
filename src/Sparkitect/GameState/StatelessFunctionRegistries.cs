using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

/// <summary>
/// Registry for per-frame stateless functions.
/// </summary>
[Registry(Identifier = "perframe_function", External = true)]
public sealed partial class PerFrameRegistry : StatelessFunctionRegistryBase, IRegistry
{
    public static string Identifier => "perframe_function";
}

/// <summary>
/// Registry for transition stateless functions.
/// </summary>
[Registry(Identifier = "transition_function", External = true)]
public sealed partial class TransitionRegistry : StatelessFunctionRegistryBase, IRegistry
{
    public static string Identifier => "transition_function";
}
