using Sparkitect.Metadata;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Metadata type for system groups. Carries scheduling/ordering
/// information for system group types.
/// Minimal placeholder for Phase 29.3 -- full content added in Phase 29.4.
/// </summary>
public class SystemGroupScheduling
{
    // Phase 29.4 will add: ordering params (OrderAfter/OrderBefore), ParentGroup, etc.
}

/// <summary>
/// Attribute applied to IHasIdentification system group types to mark them
/// for metadata generation. MetadataGenerator discovers this via
/// [MetadataCategoryMarker] and generates ApplyMetadataEntrypoint&lt;SystemGroupScheduling&gt;.
/// </summary>
[MetadataCategoryMarker]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SystemGroupSchedulingAttribute : MetadataAttribute<SystemGroupScheduling>;
