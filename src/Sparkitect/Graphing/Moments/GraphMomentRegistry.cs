using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Moments;

/// <summary>
/// Method-level value registry for graph moments — cross-pass identity. A registration binds an
/// <see cref="Identification"/> to a <see cref="MomentDefinition{T}"/> conveying the moment's resource
/// type, and nothing else: a moment declares name + resource type only, never backing, position, or
/// producer. Rides the existing RegistryGenerator (the <see cref="RegistryAttribute"/> /
/// <see cref="RegistryMethodAttribute"/> pattern) — the source generator emits one
/// <see cref="Identification"/> property per registered moment with no generator changes. The link stage
/// reads the carried resource type off the store when binding a referenced moment.
/// </summary>
[Registry(Identifier = "graph_moment")]
[PublicAPI]
public partial class GraphMomentRegistry(IGraphMomentStore store) : IRegistry
{
    /// <summary>
    /// Registers a moment: binds <paramref name="id"/> to <paramref name="definition"/>, which carries
    /// the moment's resource type. The value parameter is the non-generic <see cref="MomentDefinition"/>;
    /// mods pass a typed <see cref="MomentDefinition{T}"/> instance, which conveys the resource type at
    /// the registration site. Mirrors the method-level value-registry shape.
    /// </summary>
    [RegistryMethod]
    public void RegisterMoment(Identification id, MomentDefinition definition)
    {
        store.RegisterMoment(id, definition);
    }

    /// <inheritdoc/>
    public void Unregister(Identification id)
    {
        store.UnregisterMoment(id);
    }

    /// <summary>The registry category identifier.</summary>
    public static string Identifier => "graph_moment";
}
