using JetBrains.Annotations;
using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;

namespace Sparkitect.GameState;

[PublicAPI]
[ModuleRegistry.RegisterModule("core")]
public sealed partial class CoreModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [];
    public static Identification Identification => StateModuleID.Sparkitect.Core;
}