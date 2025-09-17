using System;
using System.Collections.Generic;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState.Samples.States;

[StateDescriptionRegistry.RegisterStateAbc("server_game")]
public class ServerGame : IStateDescriptor
{
    public static Identification ParentId => Identification.Empty; // direct child of root/bootstrap

    public static IReadOnlyList<Identification> Modules => new[]
    {
        StateModuleID.Sparkitect.Core,
        StateModuleID.Sparkitect.Game,
        StateModuleID.Sparkitect.Networking
    };
}

