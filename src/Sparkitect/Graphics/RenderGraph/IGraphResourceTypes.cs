using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Read-only catalog of registered graph-resource types and their bound managers.
/// Resolves a resource type to its <c>[ResourceManager&lt;T&gt;]</c>-declared manager type
/// by <see cref="Identification"/>. State-scope DI service shared across alternative
/// render graph implementations.
/// </summary>
[RegistryFacade<IGraphResourceTypesRegistryFacade>]
[StateFacade<IGraphResourceTypesStateFacade>]
[PublicAPI]
public interface IGraphResourceTypes
{
    /// <summary>Returns the manager type bound to <paramref name="id"/>; throws if unregistered.</summary>
    Type GetManagerTypeFor(Identification id);

    /// <summary>Non-throwing variant of <see cref="GetManagerTypeFor"/>.</summary>
    bool TryGetManagerType(Identification id, out Type managerType);
}

/// <summary>
/// Registry-context facade for <see cref="IGraphResourceTypes"/>. Used by
/// <see cref="GraphResourceRegistry"/> to record each registered resource id.
/// </summary>
[FacadeFor<IGraphResourceTypes>]
[PublicAPI]
public interface IGraphResourceTypesRegistryFacade
{
    /// <summary>Track <paramref name="id"/> as a known graph resource.</summary>
    void AddResource(Identification id);
}

/// <summary>
/// State-function facade for <see cref="IGraphResourceTypes"/>. Used by the
/// process transition to bind tracked resource ids to their manager types via
/// the metadata pipeline once registration is complete.
/// </summary>
[FacadeFor<IGraphResourceTypes>]
[PublicAPI]
public interface IGraphResourceTypesStateFacade
{
    /// <summary>
    /// Run the metadata pipeline and bind each tracked resource id to its
    /// <c>[ResourceManager&lt;T&gt;]</c>-declared manager type. Invoke once at the end
    /// of <c>process_graph_resource_registry</c>.
    /// </summary>
    void PostProcess();
}
