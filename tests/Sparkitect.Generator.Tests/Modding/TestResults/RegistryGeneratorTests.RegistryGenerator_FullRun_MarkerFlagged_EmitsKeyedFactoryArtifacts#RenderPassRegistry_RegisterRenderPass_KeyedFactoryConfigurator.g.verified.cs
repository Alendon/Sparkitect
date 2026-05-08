//HintName: RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace SampleTest.Generated;

partial class RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator
{
private void Register_RenderPassRegistry_RegisterRenderPass_KeyedFactoryConfigurator_Method(global::System.Collections.Generic.IDictionary<global::Sparkitect.Modding.Identification, global::Sparkitect.DI.IKeyedFactory<global::DiTest.IRenderPass>> registrations, global::System.Collections.Generic.IReadOnlySet<string> loadedMods)
    {
registrations[global::Sparkitect.Modding.IdentificationHelper.Read<global::DiTest.ClearColorPass>()] = new global::DiTest.ClearColorPass_KeyedFactory();
}
}
