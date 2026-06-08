using JetBrains.Annotations;
using Sparkitect.Metadata;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Non-generic resource-config base so the MetadataGenerator can match by base name. Carries the bound
/// manager type plus capability facets (currently <see cref="Publishable"/>); broaden here for future facets.
/// </summary>
[MetadataCategoryMarker]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public abstract class ResourceManagerAttribute : MetadataAttribute<ResourceManagerBinding>
{
    public abstract Type ManagerType { get; }

    /// <summary>
    /// When true, the resource is publishable through the type-routed <c>Publish&lt;T&gt;</c> door, and its
    /// manager MUST implement <c>IGraphPushTargetFor&lt;T&gt;</c> — enforced at PostRegistry.
    /// </summary>
    public virtual bool Publishable => false;
}

/// <summary>
/// Binds a resource type to the <typeparamref name="TResourceManager"/> that owns it, optionally marking it
/// <see cref="Publishable"/> for the type-routed push door.
/// </summary>
[MetadataCategoryMarker]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class ResourceManagerAttribute<TResourceManager> : ResourceManagerAttribute
    where TResourceManager : IGraphResourceManager
{
    public override Type ManagerType => typeof(TResourceManager);

    public override bool Publishable { get; }

    public ResourceManagerAttribute(bool publishable = false) => Publishable = publishable;
}
