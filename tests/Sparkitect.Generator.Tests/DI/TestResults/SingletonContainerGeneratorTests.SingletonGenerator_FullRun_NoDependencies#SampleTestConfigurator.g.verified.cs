//HintName: SampleTestConfigurator.g.cs
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace SampleTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.CoreContainerConfiguratorEntrypointAttribute]
public class SampleTestConfigurator : global::Sparkitect.DI.CoreConfigurator
{
    public override void ConfigureIoc(global::Sparkitect.DI.Container.ICoreContainerBuilder container)
    {
        container.Register<global::DiTest.TestService_Factory>();
    }
}