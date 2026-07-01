using JetBrains.Annotations;
using Sparkitect.Graphing;
using Sparkitect.Graphing.Descriptions;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Author-facing entrypoint for a pass's <c>Setup</c> phase. The single verb <see cref="Use{TResource}"/>
/// takes a resource description and returns a handle the pass holds until <c>Execute</c>, where it
/// resolves to a live instance via <see cref="IGraphResource{T}.Fetch"/>.
/// </summary>
[PublicAPI]
public interface ISetupContext
{
    /// <summary>Use <paramref name="description"/> within the active pass and return the handle to the resource it resolves to.</summary>
    IGraphResource<TResource> Use<TResource>(IResourceDescription<TResource> description);
}
