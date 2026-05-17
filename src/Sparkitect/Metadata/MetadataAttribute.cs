using JetBrains.Annotations;

namespace Sparkitect.Metadata;

/// <summary>
/// Marker attribute applied to category-specific metadata attributes.
/// Source generators scan for this marker to discover metadata categories.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class MetadataCategoryMarkerAttribute : Attribute;

/// <summary>
/// Abstract base for category-specific metadata attributes.
/// Source generators walk the base type chain to extract TMetadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[PublicAPI]
public abstract class MetadataAttribute<TMetadata> : Attribute;
