//HintName: TestModule_ServiceConfigurator.g.cs
namespace Sparkitect.CompilerGenerated.GameState;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.StateModuleServiceConfiguratorEntrypoint]
internal class TestModule_ServiceConfigurator : global::Sparkitect.GameState.IStateModuleServiceConfigurator
{
    public global::System.Type ModuleType => typeof(global::StateServiceTest.TestModule);

    public void ConfigureServices(global::Sparkitect.DI.Container.ICoreContainerBuilder builder)
    {
        builder.Register<global::StateServiceTest.TestStateService_Factory>();
    }
}
