using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>The instance-build resolution point: turns an epoch-qualified reference into the concrete dependency instance, building it dependency-first. Distinct from the pass-side <see cref="IGraphResource{T}.Fetch"/>.</summary>
[PublicAPI]
public interface IInstanceContext
{
    /// <summary>Resolves <paramref name="reference"/> to its concrete instance, building its dependencies first. The same reference resolves to the same instance within a frame.</summary>
    T Resolve<T>(ResourceRef<T> reference);

    /// <summary>Resolves a published <paramref name="moment"/> to the concrete instance of the resource that published it — the same-frame counterpart of <see cref="Resolve{T}"/> for a moment-identified resource.</summary>
    T ResolveMoment<T>(Identification moment);
}
