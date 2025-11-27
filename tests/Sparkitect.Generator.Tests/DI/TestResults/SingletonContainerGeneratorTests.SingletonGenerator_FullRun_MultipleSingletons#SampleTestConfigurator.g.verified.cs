//HintName: SampleTestConfigurator.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace SampleTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.CoreContainerConfiguratorEntrypointAttribute]
public class SampleTestConfigurator : global::Sparkitect.DI.CoreConfigurator
{
    public override void ConfigureIoc(global::Sparkitect.DI.Container.ICoreContainerBuilder container)
    {
        container.Register<global::DiTest.TestService_Factory>();
        container.Register<global::DiTest.AnotherService_Factory>();
    }
}