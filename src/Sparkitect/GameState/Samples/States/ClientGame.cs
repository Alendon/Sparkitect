using System;
using System.Collections.Generic;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState.Samples.States;

[StateDescriptionRegistry.RegisterStateAbc("client_game")]
public class ClientGame : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Desktop;

    public static IReadOnlyList<Identification> Modules => new[]
    {
        StateModuleID.Sparkitect.Core,
        StateModuleID.Sparkitect.Rendering,
        StateModuleID.Sparkitect.Game,
        StateModuleID.Sparkitect.Networking
    };
}

