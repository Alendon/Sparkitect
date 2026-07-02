using JetBrains.Annotations;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>Declares one resource: given a transaction, records its reads, increments, and sub-declarations and returns the fact that backs it. The unit an author writes to add a resource to the graph.</summary>
/// <typeparam name="T">The resource type this description produces.</typeparam>
[PublicAPI]
public interface IResourceDescription<T>
{
    /// <summary>Records this resource's declarations against <paramref name="tx"/> and returns the fact describing the produced resource.</summary>
    DeclaredFact<T> Declare(IResourceTransaction tx);
}
