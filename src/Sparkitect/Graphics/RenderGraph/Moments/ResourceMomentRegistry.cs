using JetBrains.Annotations;
using Sparkitect.Graphing.Moments;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Moments;

/// <summary>
/// Method-level value registry for resource moments — cross-pass identity over epoched resources. A
/// registration binds an <see cref="Identification"/> to a <see cref="ResourceMomentDefinition{T}"/>
/// conveying the moment's resource type only (never backing, position, or producer).
/// </summary>
[Registry(Identifier = "graph_moment")]
[PublicAPI]
public partial class ResourceMomentRegistry(IRenderGraphManagerRegistryFacade managerFacade) : IRegistry<RenderGraphModule>
{
    /// <summary>Registers a resource moment: binds <paramref name="id"/> to <paramref name="definition"/>, which carries the moment's resource type.</summary>
    [RegistryMethod]
    public void RegisterMoment(Identification id, ResourceMomentDefinition definition)
    {
        managerFacade.AddResourceMoment(id, definition);
    }

    /// <inheritdoc/>
    public void Unregister(Identification id)
    {
    }

    /// <summary>The registry category identifier.</summary>
    public static string Identifier => "graph_moment";
}
