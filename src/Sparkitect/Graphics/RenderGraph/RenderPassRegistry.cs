using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Stock pass registry. Mods register concrete <see cref="IPass"/> implementations
/// via the generator-emitted <c>[RenderPassRegistry.RegisterPass(...)]</c> attribute;
/// the keyed-factory marker drives the configurator generation consumed by
/// <see cref="Sparkitect.DI.IDIService.BuildFactoryContainer{TKey,TBase}"/>.
/// </summary>
[Registry(Identifier = "render_pass")]
[PublicAPI]
public partial class RenderPassRegistry : IRegistry
{
    [RegistryMethod]
    [KeyedFactoryGenerationMarker<IPass>]
    public void RegisterPass<TPass>(Identification id)
        where TPass : class, IPass, IHasIdentification
    {
    }

    public static string Identifier => "render_pass";

    public void Unregister(Identification id)
    {
    }
}
