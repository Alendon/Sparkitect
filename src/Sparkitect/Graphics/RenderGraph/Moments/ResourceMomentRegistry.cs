using JetBrains.Annotations;
using Sparkitect.Graphing.Moments;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Moments;

/// <summary>
/// Method-level value registry for resource moments — cross-pass identity over epoched resources. A
/// registration binds an <see cref="Identification"/> to a <see cref="ResourceMomentDefinition{T}"/>
/// conveying the moment's resource type, and nothing else: a moment declares name + resource type only,
/// never backing, position, or producer. Rides the stock RegistryGenerator (the
/// <see cref="RegistryAttribute"/> / <see cref="RegistryMethodAttribute"/> pattern) — the source
/// generator emits one <see cref="Identification"/> property per registered moment with no generator
/// changes.
/// </summary>
/// <remarks>
/// This registry is engine-integration (it needs module-driven processing + id assignment), so it lives
/// in the render-graph implementation, not in the <c>Sparkitect.Graphing</c> toolbox. Each registration
/// forwards into <see cref="IRenderGraphManagerRegistryFacade"/>, exactly as the pass and
/// render-graph-type registries do — no separate moment store service.
/// </remarks>
[Registry(Identifier = "graph_moment")]
[PublicAPI]
public partial class ResourceMomentRegistry(IRenderGraphManagerRegistryFacade managerFacade) : IRegistry
{
    /// <summary>
    /// Registers a resource moment: binds <paramref name="id"/> to <paramref name="definition"/>, which
    /// carries the moment's resource type. Mods pass a typed <see cref="ResourceMomentDefinition{T}"/>
    /// instance, which conveys the resource type at the registration site.
    /// </summary>
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
