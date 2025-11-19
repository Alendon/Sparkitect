//HintName: RegistryFacadeConfigurator.g.cs
namespace Sparkitect.Modding.CompilerGenerated.DI;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.FacadeConfiguratorEntrypoint<global::Sparkitect.Modding.RegistryFacadeAttribute>]
internal class GeneratedRegistryFacadeConfigurator : global::Sparkitect.Modding.IRegistryFacadeConfigurator
{
    public void ConfigureFacades(global::Sparkitect.DI.IFacadeHolder facadeHolder)
    {

        facadeHolder.AddFacade(typeof(global::FacadeTest.IRegistryFacade), typeof(global::FacadeTest.ITestService));

    }
}
