using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>
/// The instance-build resolution point. <see cref="Resolve{T}"/> turns an epoch-qualified reference
/// into the concrete dependency instance, building it dependency-first. This is distinct from the
/// pass-side <see cref="IGraphResource{T}.Fetch"/> frame-fetch: resolution here builds the object a
/// reference points at, it cannot consult the originating description, and it operates on a single
/// in-flight frame (N=1).
/// </summary>
[PublicAPI]
public interface IInstanceContext
{
    /// <summary>
    /// Resolves <paramref name="reference"/> to its concrete instance, building the dependency (and
    /// its dependencies) first. The same reference resolves to the same instance within a frame.
    /// </summary>
    T Resolve<T>(ResourceRef<T> reference);
}
