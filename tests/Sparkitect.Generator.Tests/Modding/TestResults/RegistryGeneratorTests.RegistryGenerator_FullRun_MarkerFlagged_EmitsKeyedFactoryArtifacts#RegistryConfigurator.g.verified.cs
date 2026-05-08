//HintName: RegistryConfigurator.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace SampleTest.Generated;

partial class RegistryConfigurator
{
private void RegisterRegistries(global::System.Collections.Generic.IDictionary<string, global::Sparkitect.DI.IKeyedFactory<global::Sparkitect.Modding.IRegistryBase>> registrations, global::System.Collections.Generic.IReadOnlySet<string> loadedMods)
    {
registrations["render_pass"] = new global::DiTest.RenderPassRegistry_KeyedFactory();
}
}
