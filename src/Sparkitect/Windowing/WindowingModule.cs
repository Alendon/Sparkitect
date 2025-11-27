using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.Windowing;

[ModuleRegistry.RegisterModule("windowing")]
public partial class WindowingModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];

    public static Identification Identification => StateModuleID.Sparkitect.Windowing;
    
    
}