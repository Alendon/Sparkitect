using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Hand-written publish surface for <see cref="EntityListResource"/>. Forwards through
/// <see cref="IRenderGraph.GetHandler{THandler}"/> to the graph's <see cref="IExternalResourceHandler"/>,
/// which routes by the resource type's <c>Identification</c>; throws if the graph exposes no handler.
/// </summary>
[PublicAPI]
public static class EntityListResourceExtensions
{
    extension(EntityListResource resource)
    {
        public void Apply(IRenderGraph renderGraph)
        {
            var handler = renderGraph.GetHandler<IExternalResourceHandler>()
                ?? throw new InvalidOperationException(
                    "EntityListResource.Apply: the render graph does not expose an IExternalResourceHandler.");
            handler.Publish(resource);
        }
    }
}
