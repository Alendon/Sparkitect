//HintName: TestModule_init_Wrapper.g.cs
// Expected SG output for TestModule.Initialize - Transition function (OnCreate)
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace GameStateTest;

public partial class TestModule
{
    internal class initWrapper : global::Sparkitect.Stateless.IStatelessFunction
    {
        public static global::Sparkitect.Modding.Identification ParentId
            => global::GameStateTest.TestModule.Identification;

        private global::GameStateTest.ITestService _param0;

        [global::System.Diagnostics.DebuggerStepThroughAttribute]
        public void Execute()
        {
            TestModule.Initialize(_param0);
        }

        public void Initialize(
            global::Sparkitect.DI.Container.ICoreContainer container,
            global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Type> facadeMap)
        {
            if (!container.TryResolveMapped<global::GameStateTest.ITestService>(out _param0, facadeMap))
            {
                throw new global::System.InvalidOperationException(
                    "Failed to resolve global::GameStateTest.ITestService for stateless function init");
            }
        }
    }
}