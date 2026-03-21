using Sparkitect.Metadata;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Metadata type for system groups. Carries ordering constraints and parent group relationship.
/// Constructor parameters are matched by MetadataExtractionPipeline against class-level attributes.
/// </summary>
public class SystemGroupScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;
    private readonly ParentIdAttribute? _parentId;

    public IReadOnlyList<OrderAfterAttribute> OrderAfter => _orderAfter;
    public IReadOnlyList<OrderBeforeAttribute> OrderBefore => _orderBefore;
    public Identification? ParentGroupId => _parentId?.Other;

    public SystemGroupScheduling(
        OrderAfterAttribute[] orderAfter,
        OrderBeforeAttribute[] orderBefore,
        ParentIdAttribute? parentId)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
        _parentId = parentId;
    }
}

/// <summary>
/// Attribute applied to IHasIdentification system group types to mark them
/// for metadata generation. MetadataGenerator discovers this via
/// [MetadataCategoryMarker] and generates ApplyMetadataEntrypoint&lt;SystemGroupScheduling&gt;.
/// </summary>
[MetadataCategoryMarker]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SystemGroupSchedulingAttribute : MetadataAttribute<SystemGroupScheduling>;
