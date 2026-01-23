using Sparkitect.Modding;

namespace Sparkitect.Stateless;

public interface IStatelessFunctionManager
{
    // High-level: get sorted functions for a scheduling category
    IReadOnlyList<IStatelessFunction> GetSorted<TScheduling, TContext, TRegistry>(
        IEnumerable<Identification> owners) where TScheduling : IScheduling<TContext, TRegistry>
        where TContext : class
        where TRegistry : IRegistry;

    // Low-level: access to graph building
    IExecutionGraphBuilder CreateGraphBuilder<TScheduling, TContext, TRegistry>()
        where TScheduling : IScheduling<TContext, TRegistry> where TContext : class where TRegistry : IRegistry;
}
