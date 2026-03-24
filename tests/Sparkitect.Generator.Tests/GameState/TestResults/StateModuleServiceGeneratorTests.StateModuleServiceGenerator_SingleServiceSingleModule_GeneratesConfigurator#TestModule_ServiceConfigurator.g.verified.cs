//HintName: TestModule_ServiceConfigurator.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace StateServiceTest.Generated.GameState;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.StateModuleServiceConfiguratorEntrypointAttribute]
internal class TestModule_ServiceConfigurator : global::Sparkitect.GameState.IStateModuleServiceConfigurator
{
public global::System.Type ModuleType => typeof(global::StateServiceTest.TestModule);

public void Configure(global::Sparkitect.DI.Container.ICoreContainerBuilder builder, global::System.Collections.Generic.IReadOnlySet<string> loadedMods)
    {
builder.Register<global::StateServiceTest.TestStateService_Factory>();
}
}
