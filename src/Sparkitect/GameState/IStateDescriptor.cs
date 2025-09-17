using System.Collections.Generic;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

public interface IStateDescriptor
{
    static abstract Identification ParentId { get; }

    static abstract IReadOnlyList<Identification> Modules { get; }
}

public interface IStateDescriptorMethods
{
    public IReadOnlyList<IStateMethod> ContainingMethods { get; }
}

public interface IStateMethod
{
    public void Execute();
    public void Initialize(IStateContainer container);
    
    /*
     * Add the metadata here for the methods. To be able to properly sort them.
     * Source Generation
     */
}

