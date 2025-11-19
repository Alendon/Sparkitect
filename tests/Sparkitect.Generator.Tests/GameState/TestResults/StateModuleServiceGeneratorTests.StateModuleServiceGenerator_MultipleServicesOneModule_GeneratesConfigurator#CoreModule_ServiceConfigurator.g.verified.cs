//HintName: CoreModule_ServiceConfigurator.g.cs
namespace Sparkitect.CompilerGenerated.GameState;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.StateModuleServiceConfiguratorEntrypoint]
internal class CoreModule_ServiceConfigurator : global::Sparkitect.GameState.IStateModuleServiceConfigurator
{
    public global::System.Type ModuleType => typeof(global::StateServiceTest.CoreModule);

    public void ConfigureServices(global::Sparkitect.DI.Container.ICoreContainerBuilder builder)
    {
        builder.Register<global::StateServiceTest.ServiceA_Factory>();
        builder.Register<global::StateServiceTest.ServiceB_Factory>();
        builder.Register<global::StateServiceTest.ServiceC_Factory>();
    }
}
