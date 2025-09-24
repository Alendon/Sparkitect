
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState.Samples.States;

[StateDescriptionRegistry.RegisterStateAbc("main_menu")]
public class MainMenu : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Desktop;
    public static Identification Identification => StateID.Sparkitect.MainMenu;

    public static IReadOnlyList<Identification> Modules => [];
}

