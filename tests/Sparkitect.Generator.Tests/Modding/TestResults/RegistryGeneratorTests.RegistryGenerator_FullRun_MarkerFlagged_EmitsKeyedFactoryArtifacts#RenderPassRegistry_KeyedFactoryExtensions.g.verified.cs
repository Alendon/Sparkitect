//HintName: RenderPassRegistry_KeyedFactoryExtensions.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace SampleTest.Generated.KeyedFactoryExtensions;

public static class RenderPassRegistryKeyedFactoryExtensions
{
    extension(global::DiTest.RenderPassRegistry)
    {
        public static global::System.Type RegisterRenderPassConfiguratorAttribute
            => typeof(global::SampleTest.Generated.RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfiguratorAttribute);

        public static global::Sparkitect.DI.Container.IFactoryContainer<global::Sparkitect.Modding.Identification, global::DiTest.IRenderPass> BuildRegisterRenderPassContainer(
            global::Sparkitect.DI.IDIService di,
            global::Sparkitect.DI.Container.ICoreContainer container,
            global::Sparkitect.DI.Resolution.IResolutionProvider? provider,
            global::System.Collections.Generic.IEnumerable<string> modIds)
            => di.BuildFactoryContainer<global::Sparkitect.Modding.Identification, global::DiTest.IRenderPass>(
                container,
                provider,
                modIds,
                typeof(global::SampleTest.Generated.RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfiguratorAttribute));
    }
}