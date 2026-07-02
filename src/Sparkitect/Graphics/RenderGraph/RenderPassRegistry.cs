using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Stock pass registry. Mods register concrete <see cref="IPass"/> implementations via the
/// generator-emitted <c>[RenderPassRegistry.RegisterPass(...)]</c> attribute.
/// </summary>
[Registry(Identifier = "render_pass")]
[PublicAPI]
public partial class RenderPassRegistry(IRenderGraphManagerRegistryFacade managerFacade) : IRegistry<RenderGraphModule>
{
    /// <summary>Registers pass <typeparamref name="TPass"/> under <paramref name="id"/>; called by generated code from the <c>[RegisterPass]</c> attribute, not directly.</summary>
    [RegistryMethod]
    [KeyedFactoryGenerationMarker<IPass>]
    public void RegisterPass<TPass>(Identification id)
        where TPass : class, IPass, IHasIdentification
    {
        managerFacade.AddPass(id);
    }

    /// <summary>The registry's stable identifier.</summary>
    public static string Identifier => "render_pass";

    /// <summary>Removes the pass registered under <paramref name="id"/>. No-op: passes are not unregistered at runtime.</summary>
    public void Unregister(Identification id)
    {
    }
}
