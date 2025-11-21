using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState;

/// <summary>
/// Root game state - semantic anchor for state hierarchy.
/// Never instantiated as a stack frame, but required for validation.
/// All other states must have a parent chain that terminates at Root.
/// </summary>
[StateRegistry.RegisterState("root")]
public partial class RootGameStateDescriptor : IStateDescriptor
{
    public static Identification ParentId => Identification.Empty;
    public static IReadOnlyList<Identification> Modules => [StateModuleID.Sparkitect.Core];
    public static Identification Identification => StateID.Sparkitect.Root;
}
