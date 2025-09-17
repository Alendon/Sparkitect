using System;
using System.Collections.Generic;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.CompilerGenerated.IdExtensions;

namespace Sparkitect.GameState.Samples.States;

[StateDescriptionRegistry.RegisterStateAbc("local_game")]
public class LocalGame : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Desktop;

    public static IReadOnlyList<Identification> Modules => new[]
    {
        StateModuleID.Sparkitect.Core,
        StateModuleID.Sparkitect.Rendering,
        StateModuleID.Sparkitect.Game
    };
}

