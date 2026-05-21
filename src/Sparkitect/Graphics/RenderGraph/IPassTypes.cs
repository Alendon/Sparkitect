using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Read-only catalog of registered pass types. Parallel shape to
/// <see cref="IGraphResourceTypes"/>. Today exposes the registered-pass set; the
/// home for any per-pass-type metadata that accumulates beyond what
/// <see cref="RenderPassRegistry"/> + its keyed factory already provide.
/// </summary>
[RegistryFacade<IPassTypesRegistryFacade>]
[PublicAPI]
public interface IPassTypes
{
    /// <summary>All pass identifications registered with the render graph.</summary>
    IReadOnlyCollection<Identification> RegisteredPassIds { get; }
}

/// <summary>
/// Registry-context facade for <see cref="IPassTypes"/>. Used by
/// <see cref="RenderPassRegistry"/> to record each registered pass id.
/// </summary>
[FacadeFor<IPassTypes>]
[PublicAPI]
public interface IPassTypesRegistryFacade
{
    /// <summary>Track <paramref name="id"/> as a known pass.</summary>
    void AddPass(Identification id);
}
