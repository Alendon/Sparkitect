using System;
using System.Collections.Generic;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.GameState.Samples.States;

[StateRegistry.RegisterState("desktop")]
public class Desktop : IStateDescriptor
{
    public static Identification ParentId => Identification.Empty;

    public static IReadOnlyList<Identification> Modules => Array.Empty<Identification>();

    public static IReadOnlyDictionary<Identification, StateActivationPolicy>? Activation => null;
}
