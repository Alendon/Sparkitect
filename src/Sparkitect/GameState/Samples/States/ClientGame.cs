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
    public static Identification Identification => StateID.Sparkitect.ClientGame;

    public static IReadOnlyList<Identification> Modules => new[]
    {
        //Core and Rendering, are not needed to be specifically declared, as they live already in the parent desktop state
        /*StateModuleID.Sparkitect.Core,
        StateModuleID.Sparkitect.Rendering,*/
        StateModuleID.Sparkitect.Ecs,
        StateModuleID.Sparkitect.Networking
    };
}

