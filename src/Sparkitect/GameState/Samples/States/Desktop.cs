using System;
using System.Collections.Generic;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.CompilerGenerated.IdExtensions;

namespace Sparkitect.GameState.Samples.States;

[StateDescriptionRegistry.RegisterStateAbc("desktop")]
public class Desktop : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Entry;
    public static Identification Identification => StateID.Sparkitect.Desktop;

    public static IReadOnlyList<Identification> Modules => new[]
    {
        // Rendering baseline for desktop states (Core is declared in EntryState)
        StateModuleID.Sparkitect.Rendering
    };
}
