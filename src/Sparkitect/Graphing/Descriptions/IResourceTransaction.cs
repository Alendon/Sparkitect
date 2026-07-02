using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>The declaration-time verb surface a resource description records through. Every verb is valid only inside a description's <see cref="IResourceDescription{T}.Declare"/>; the enclosing resource is the transaction's current self.</summary>
[PublicAPI]
public interface IResourceTransaction
{
    /// <summary>Records a consuming read of <paramref name="reference"/>'s epoch, ordering the current declaration after that epoch's producer.</summary>
    void Read<T>(ResourceRef<T> reference);

    /// <summary>Advances <paramref name="reference"/>'s chain one symbolic epoch and returns a ref to the produced epoch — the write verb.</summary>
    ResourceRef<T> Increment<T>(ResourceRef<T> reference);

    /// <summary>Increments <paramref name="reference"/> and tags the produced epoch with <paramref name="moment"/> so later declarations can reference it by name.</summary>
    ResourceRef<T> Increment<T>(ResourceRef<T> reference, Identification moment);

    /// <summary>Records a read-dependency on a named <paramref name="moment"/> without producing an epoch.</summary>
    void ReferenceMoment(Identification moment);

    /// <summary>Declares a nested sub-resource by running <paramref name="description"/> in this same transaction, returning a structural ref to it. Each description instance may be declared once.</summary>
    ResourceRef<TSub> Declare<TSub>(IResourceDescription<TSub> description);

    /// <summary>Returns the base-epoch ref to the resource the current description is declaring.</summary>
    ResourceRef<T> Self<T>();

    /// <summary>Resolves the DI-backed fact instance registered for <typeparamref name="TDeclaredFact"/>; requires the transaction to carry a fact factory.</summary>
    TDeclaredFact InstantiateFact<TDeclaredFact>() where TDeclaredFact : DeclaredFact, IHasIdentification;
}
