using JetBrains.Annotations;
using Sparkitect.Metadata;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Non-generic base so the MetadataGenerator can match by base name; carries the manager type.</summary>
[MetadataCategoryMarker]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public abstract class ResourceManagerAttribute : MetadataAttribute<ResourceManagerBinding>
{
    public abstract Type ManagerType { get; }
}

/// <summary>Binds a resource type to the <typeparamref name="TResourceManager"/> that owns it.</summary>
[MetadataCategoryMarker]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class ResourceManagerAttribute<TResourceManager> : ResourceManagerAttribute
    where TResourceManager : IGraphResourceManager
{
    public override Type ManagerType => typeof(TResourceManager);
}
