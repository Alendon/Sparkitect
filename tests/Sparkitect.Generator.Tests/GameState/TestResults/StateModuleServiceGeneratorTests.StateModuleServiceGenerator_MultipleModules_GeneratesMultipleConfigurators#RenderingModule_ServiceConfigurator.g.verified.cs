//HintName: RenderingModule_ServiceConfigurator.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace Sparkitect.CompilerGenerated.GameState;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.StateModuleServiceConfiguratorEntrypoint]
internal class RenderingModule_ServiceConfigurator : global::Sparkitect.GameState.IStateModuleServiceConfigurator
{
    public global::System.Type ModuleType => typeof(global::StateServiceTest.RenderingModule);

    public void ConfigureServices(global::Sparkitect.DI.Container.ICoreContainerBuilder builder)
    {
        builder.Register<global::StateServiceTest.RenderService_Factory>();
    }
}
