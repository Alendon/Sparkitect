using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// Metadata-table value carrying the resource type's bound manager. Mirrors the
/// <c>SystemGroupScheduling</c> indirection — the ctor arg is the attribute the
/// <c>MetadataGenerator</c> harvested off the resource type.
/// </summary>
[PublicAPI]
public sealed record ResourceManagerBinding(ResourceManagerAttribute? Attribute)
{
    public Type ManagerType => Attribute?.ManagerType
        ?? throw new InvalidOperationException(
            "ResourceManagerBinding has no [ResourceManager<…>] attribute applied to its target type.");

    /// <summary>Whether the bound resource is publishable through the type-routed push door.</summary>
    public bool Publishable => Attribute?.Publishable ?? false;
}
