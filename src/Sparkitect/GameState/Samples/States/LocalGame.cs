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
    public static Identification Identification => StateID.Sparkitect.LocalGame;

    public static IReadOnlyList<Identification> Modules =>
    [
        StateModuleID.Sparkitect.Ecs
    ];
}

