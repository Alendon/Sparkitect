using Sparkitect.Modding;

namespace Sparkitect.Stateless;

public interface IScheduling
{
    
}

public interface IScheduling<TContext, TKey> : IScheduling where TKey : unmanaged
{
    void BuildGraph(IExecutionGraphBuilder<TKey> buildGraph, TransitionContext context);
    
}

// Marks a Scheduling implementation for configuring the behaviour of the source generator related to this scheduling
public abstract class SchedulingAttribute : Attribute
{
    
}

//Provide the Identification 
public abstract class SchedulingAttribute<TIdentifiable> : SchedulingAttribute where TIdentifiable : IHasIdentification;