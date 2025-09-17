using System.Collections.Generic;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

public interface IStateDescriptor
{
    static abstract Identification ParentId { get; }

    static abstract IReadOnlyList<Identification> Modules { get; }

    static abstract IReadOnlyDictionary<Identification, StateActivationPolicy>? Activation { get; }
}
