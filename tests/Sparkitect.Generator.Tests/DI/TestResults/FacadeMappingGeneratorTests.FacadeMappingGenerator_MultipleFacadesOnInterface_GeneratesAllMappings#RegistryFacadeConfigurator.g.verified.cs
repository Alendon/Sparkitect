//HintName: RegistryFacadeConfigurator.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace FacadeTest.Generated.CompilerGenerated.DI;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.FacadeConfiguratorEntrypoint<global::Sparkitect.Modding.RegistryFacadeAttribute>]
internal class GeneratedRegistryFacadeConfigurator : global::Sparkitect.Modding.IRegistryFacadeConfigurator
{
    public void ConfigureFacades(global::Sparkitect.DI.IFacadeHolder facadeHolder)
    {

        facadeHolder.AddFacade(typeof(global::FacadeTest.IRegistryFacade), typeof(global::FacadeTest.ITestService));

    }
}
