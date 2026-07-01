using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Stock pass registry. Mods register concrete <see cref="IPass"/> implementations
/// via the generator-emitted <c>[RenderPassDeprecatedRegistry.RegisterPass(...)]</c> attribute;
/// the keyed-factory marker drives the configurator generation consumed by
/// <see cref="Sparkitect.DI.IDIService.BuildFactoryContainer{TKey,TBase}"/>.
/// Each registration call forwards the id into <see cref="IRenderGraphManagerRegistryFacade"/>
/// for tracking.
/// </summary>
[Registry(Identifier = "render_pass_deprecated")]
[PublicAPI]
public partial class RenderPassDeprecatedRegistry(IRenderGraphManagerRegistryFacade managerFacade) : IRegistry<RenderGraphDeprecatedModule>
{
    [RegistryMethod]
    [KeyedFactoryGenerationMarker<IPass>]
    public void RegisterPass<TPass>(Identification id)
        where TPass : class, IPass, IHasIdentification
    {
        managerFacade.AddPass(id);
    }

    public static string Identifier => "render_pass_deprecated";

    public void Unregister(Identification id)
    {
    }
}
