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
    [RegistryMethod]
    [KeyedFactoryGenerationMarker<IPass>]
    public void RegisterPass<TPass>(Identification id)
        where TPass : class, IPass, IHasIdentification
    {
        managerFacade.AddPass(id);
    }

    public static string Identifier => "render_pass";

    public void Unregister(Identification id)
    {
    }
}
