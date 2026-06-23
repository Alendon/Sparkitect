using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>
/// The description-internal grammar a description speaks inside its <see cref="IResourceDescription{T}.Declare"/>.
/// It exposes the two relations one level down — a <see cref="Read{T}"/> of an epoch-qualified
/// reference (an ordering/consumer edge) and an <see cref="Increment{T}"/> of a reference (advancing
/// the resource one epoch and minting the post-increment reference) — plus a recursive
/// <see cref="Declare{TSub}"/> sub-declaration that runs a sub-description inside the same transaction.
/// The same Increment mechanic serves both sub-resources and the resource the description itself
/// resolves to; there is no separate "advance myself" verb. The transaction never resolves epoch
/// positions — references are handed out symbolic and resolved later, in the Link phase.
/// </summary>
[PublicAPI]
public interface IResourceTransaction
{
    /// <summary>
    /// Records a Read of an already-minted reference, creating a consumer edge against that epoch.
    /// The read epoch must have a producing increment to be schedulable; the base epoch is holdable
    /// but never readable.
    /// </summary>
    void Read<T>(ResourceRef<T> reference);

    /// <summary>
    /// Advances the referenced resource one epoch and returns the minted post-increment reference.
    /// The same mechanic applies whether <paramref name="reference"/> points at a sub-resource or at
    /// the resource the description itself resolves to (self-increment is just an Increment).
    /// </summary>
    ResourceRef<T> Increment<T>(ResourceRef<T> reference);

    /// <summary>
    /// Advances the referenced resource one epoch (the ordinary Increment) and marks the produced
    /// increment with <paramref name="moment"/> — a produce-usage description publishes a moment by
    /// passing the moment it received as a constructor <see cref="Sparkitect.Modding.Identification"/>.
    /// There is no separate pass-level marking verb; marking rides this Increment.
    /// </summary>
    ResourceRef<T> Increment<T>(ResourceRef<T> reference, Identification moment);

    /// <summary>
    /// References a moment by id — a consume-usage description reads a moment it received as a
    /// constructor <see cref="Sparkitect.Modding.Identification"/>. The Link phase binds the reference
    /// to the single marked increment; zero or two marked increments are diagnostics.
    /// </summary>
    void ReferenceMoment(Identification moment);

    /// <summary>
    /// Sub-declares a composite part: runs <paramref name="description"/>'s <c>Declare</c> recursively
    /// inside this same transaction and returns the sub-resource's base-epoch reference. Composition
    /// is recursive declaration.
    /// </summary>
    ResourceRef<TSub> Declare<TSub>(IResourceDescription<TSub> description);

    /// <summary>
    /// The base-epoch reference to the resource the currently-declaring description itself resolves to
    /// — the node minted for it when it was declared. A description self-increments by passing
    /// this reference to <see cref="Increment{T}"/>; there is no separate "advance myself" verb.
    /// </summary>
    /// <typeparam name="T">The resource type the current description resolves to.</typeparam>
    ResourceRef<T> Self<T>();
}
