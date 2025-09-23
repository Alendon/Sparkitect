using System;
using System.Collections.Generic;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState.Samples.States;

[StateDescriptionRegistry.RegisterStateAbc("server_game")]
public class ServerGame : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Entry;

    public static IReadOnlyList<Identification> Modules =>
        [StateModuleID.Sparkitect.Networking, StateModuleID.Sparkitect.Ecs];
}

