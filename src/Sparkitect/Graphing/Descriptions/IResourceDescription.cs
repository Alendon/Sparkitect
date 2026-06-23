using JetBrains.Annotations;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>
/// A resource declaration: its <see cref="Declare"/> runs exactly once inside a transaction, speaks
/// the description-internal two-relation grammar (Read / Increment / sub-declaration), and returns
/// immutable <see cref="DeclaredFacts{T}"/> that own instance creation. A description encodes one use
/// case (a writer and a reader use different descriptions); its resource type <typeparamref name="T"/>
/// is carried by C# generic shape — there is no per-type registry.
/// </summary>
/// <typeparam name="T">The resource type this description resolves to.</typeparam>
[PublicAPI]
public interface IResourceDescription<T>
{
    /// <summary>
    /// Runs the declaration logic once against <paramref name="tx"/> and returns the immutable facts.
    /// The facts hold the references this declaration minted/sub-declared and own the construction of
    /// the live instance; the description is discarded once its facts land in the ledger.
    /// </summary>
    DeclaredFacts<T> Declare(IResourceTransaction tx);
}
