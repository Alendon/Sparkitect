using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;

namespace Sparkitect.GameState;

[StateDescriptionRegistry.RegisterStateAbc("root")]
public partial class RootGameStateDescriptor : IStateDescriptor
{
    public static Identification ParentId => Identification.Empty;
    public static IReadOnlyList<Identification> Modules => [StateModuleID.Sparkitect.Core];
}