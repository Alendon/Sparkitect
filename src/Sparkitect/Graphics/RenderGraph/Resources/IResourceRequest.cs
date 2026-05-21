using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Marker for declaration-shape requests routed to a resource manager.</summary>
[PublicAPI]
public interface IResourceRequest<TResource>;
