//HintName: TestModule_TransitionScheduling.g.cs
// Expected SG output for TestModule transition function scheduling
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace GameStateTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.Stateless.ApplySchedulingEntrypointAttribute<global::Sparkitect.Stateless.TransitionFunctionAttribute>]
internal class TestModule_TransitionScheduling
    : global::Sparkitect.Stateless.ApplySchedulingEntrypoint<
        global::Sparkitect.Stateless.TransitionFunctionAttribute,
        global::Sparkitect.Stateless.TransitionContext,
        global::Sparkitect.Stateless.TransitionRegistry>
{
    public override void BuildGraph(
        global::Sparkitect.Stateless.IExecutionGraphBuilder builder,
        global::Sparkitect.Stateless.TransitionContext context)
    {
        // TestModule.Initialize - OnCreate
        // Ordering: runs before AnotherModule.Process, runs before TestModule.Update
        new global::Sparkitect.Stateless.OnCreateScheduling(
            new global::Sparkitect.Stateless.OrderBeforeAttribute<global::GameStateTest.AnotherModule>("process"),
            new global::Sparkitect.Stateless.OrderBeforeAttribute("update")
        ).BuildGraph(builder, context, global::GameStateTest.IDs.TransitionId.TestModule_init);

        // TestModule.Cleanup - OnDestroy
        // Ordering: runs before TestModule.Update
        new global::Sparkitect.Stateless.OnDestroyScheduling(
            new global::Sparkitect.Stateless.OrderBeforeAttribute("update")
        ).BuildGraph(builder, context, global::GameStateTest.IDs.TransitionId.TestModule_cleanup);
    }
}
