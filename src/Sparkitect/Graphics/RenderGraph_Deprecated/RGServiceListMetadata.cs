using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Per-render-graph-type list of required service interfaces. Looked up by render-graph
/// identification; the manager registers the corresponding factories into the per-graph
/// child container at construction time.
/// </summary>
/// <param name="ServiceInterfaces">The interface types the render graph requires.</param>
[PublicAPI]
public sealed record RGServiceListMetadata(IReadOnlyList<Type> ServiceInterfaces);
