using JetBrains.Annotations;
using Sparkitect.Graphing;
using Sparkitect.Graphing.Descriptions;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Author-facing entrypoint for a pass's <c>Setup</c> phase. The single verb is
/// <see cref="Use{TResource}"/>: a pass hands in a resource description and receives a logical handle
/// it holds onto until <c>Execute</c>. The handle resolves to a live instance via
/// <see cref="IGraphResource{T}.Fetch"/> only once execution begins.
/// </summary>
[PublicAPI]
public interface ISetupContext
{
    /// <summary>
    /// Use <paramref name="description"/> within the active pass and return the handle to the
    /// resource it resolves to. The description's declaration runs inside the setup transaction;
    /// the returned handle is opaque and carries no declaration-internal wiring.
    /// </summary>
    IGraphResource<TResource> Use<TResource>(IResourceDescription<TResource> description);
}
