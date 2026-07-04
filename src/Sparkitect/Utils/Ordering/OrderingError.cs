using System.Text;
using JetBrains.Annotations;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Utils.Ordering;

/// <summary>
/// Ordering-failure diagnostics returned by <c>OrderingGraphBuilder&lt;TNode&gt;.Sort</c>.
/// The ordering core never throws for these conditions; each case carries the provenance a
/// consumer needs to raise its own domain-specific error at its unwrap site.
/// </summary>
/// <typeparam name="TNode">The graph node key type (for example a type name, identification, or CLR type).</typeparam>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record OrderingError<TNode>
{
    /// <summary>A cycle in the ordering graph, naming every participating node.</summary>
    /// <param name="Participants">The nodes that form the cycle, in stable add-order.</param>
    public sealed partial record Cycle(IReadOnlyList<TNode> Participants) : OrderingError<TNode>
    {
        /// <summary>Renders the participant ids instead of the backing list's type name.</summary>
        protected override bool PrintMembers(StringBuilder builder)
        {
            builder.Append("Participants = [").Append(string.Join(", ", Participants)).Append(']');
            return true;
        }
    }

    /// <summary>A required edge whose endpoint is not a known node, naming both endpoints.</summary>
    /// <param name="From">The edge source.</param>
    /// <param name="To">The edge target.</param>
    public sealed partial record MissingRequiredDependency(TNode From, TNode To) : OrderingError<TNode>;
}
