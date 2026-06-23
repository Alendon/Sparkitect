using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Author-facing entrypoint for declaring graph resources during a pass's <c>Setup</c> phase.
/// </summary>
[PublicAPI]
public interface ISetupContext
{
    /// <summary>
    /// Declares a typed graph resource for the active pass and returns a logical handle the
    /// pass holds onto until <c>Execute</c> / <c>PreExecute</c>. <c>Declare</c> is exclusively
    /// a <c>Setup</c>-phase API; the returned handle resolves to a live view via
    /// <see cref="IGraphResource{TView}.Fetch"/> only once execution begins.
    /// </summary>
    IGraphResource<TResource> Declare<TResource>(IResourceRequest<TResource> request)
        where TResource : IHasIdentification;
}
