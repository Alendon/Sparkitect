//HintName: StateFacadeConfigurator.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace Sparkitect.GameState.CompilerGenerated.DI;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.FacadeConfiguratorEntrypoint<global::Sparkitect.GameState.StateFacadeAttribute>]
internal class GeneratedStateFacadeConfigurator : global::Sparkitect.GameState.IStateFacadeConfigurator
{
    public void ConfigureFacades(global::Sparkitect.DI.IFacadeHolder facadeHolder)
    {

        facadeHolder.AddFacade(typeof(global::FacadeTest.IServiceAFacade), typeof(global::FacadeTest.IServiceA));

        facadeHolder.AddFacade(typeof(global::FacadeTest.IServiceBFacade), typeof(global::FacadeTest.IServiceB));

    }
}
