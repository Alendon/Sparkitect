//HintName: CoreModule_ServiceConfigurator.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace Sparkitect.CompilerGenerated.GameState;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.StateModuleServiceConfiguratorEntrypoint]
internal class CoreModule_ServiceConfigurator : global::Sparkitect.GameState.IStateModuleServiceConfigurator
{
    public global::System.Type ModuleType => typeof(global::StateServiceTest.CoreModule);

    public void ConfigureServices(global::Sparkitect.DI.Container.ICoreContainerBuilder builder)
    {
        builder.Register<global::StateServiceTest.CoreService_Factory>();
    }
}
