using System.Collections.Generic;
using System.ComponentModel;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

public interface IStateDescriptor
{
    static abstract Identification ParentId { get; }
    static abstract Identification Identification { get; }

    static abstract IReadOnlyList<Identification> Modules { get; }
}

public interface IStateDescriptorMethods
{
    public IReadOnlyList<IStateMethod> ContainingMethods { get; }
}


public interface IStateMethod
{
    public void Execute();
    public void Initialize(IFacadedCoreContainer container);
}

