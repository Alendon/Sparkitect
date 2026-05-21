using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>Author-facing entrypoint for declaring graph resources during a pass's Setup phase.</summary>
[PublicAPI]
public interface ISetupContext
{
    IGraphResource<TResource> Declare<TResource>(IResourceRequest<TResource> request)
        where TResource : IHasIdentification;
}
