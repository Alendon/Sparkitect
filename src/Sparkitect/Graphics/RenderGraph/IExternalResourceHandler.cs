using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Receives external resources published into a render graph from outside the pass
/// <c>Setup</c> pipeline. Singleton cardinality only — one value per resource type;
/// re-publishing the same type replaces the previously published value. Keyed / list
/// cardinality is intentionally out of scope until a concrete consumer needs it.
/// </summary>
[PublicAPI]
public interface IExternalResourceHandler
{
    void Publish<TResource>(TResource value) where TResource : IHasIdentification;
}
