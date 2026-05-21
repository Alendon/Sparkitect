using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>Marker for graph-resource fields/properties on a pass; consumed by a future slot-wiring generator.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class GraphResourceAttribute : Attribute;
