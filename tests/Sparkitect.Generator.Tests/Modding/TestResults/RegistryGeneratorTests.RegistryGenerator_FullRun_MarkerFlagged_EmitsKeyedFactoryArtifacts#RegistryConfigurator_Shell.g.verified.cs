//HintName: RegistryConfigurator_Shell.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace SampleTest.Generated;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.RegistryConfigurator]
internal partial class RegistryConfigurator : global::Sparkitect.DI.IRegistryConfigurator
{
    public void Configure(
        global::System.Collections.Generic.IDictionary<string, global::Sparkitect.DI.IKeyedFactory<global::Sparkitect.Modding.IRegistryBase>> registrations,
        global::System.Collections.Generic.IReadOnlySet<string> loadedMods)
    {
        RegisterRegistries(registrations, loadedMods);
    }
}