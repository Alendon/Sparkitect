//HintName: TestModule_TransitionScheduling.g.cs
// Expected SG output for TestModule transition function scheduling

using Sparkitect.Modding;

#pragma warning disable CS9113
#pragma warning disable CS1591

namespace GameStateTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.ApplySchedulingEntrypointAttribute<global::Sparkitect.Stateless.TransitionFunctionAttribute>]
internal class TestModule_TransitionScheduling
    : global::Sparkitect.GameState.ApplySchedulingEntrypoint<
        global::Sparkitect.Stateless.TransitionFunctionAttribute,
        global::Sparkitect.GameState.TransitionContext,
        global::Sparkitect.GameState.TransitionRegistry>
{
    public override void BuildGraph(
        global::Sparkitect.Stateless.IExecutionGraphBuilder builder,
        global::Sparkitect.GameState.TransitionContext context)
    {
        // TestModule.Initialize - OnCreate
        // Ordering: runs before AnotherModule.Process, runs before TestModule.Update
        new global::Sparkitect.GameState.OnCreateScheduling(
            new global::Sparkitect.Stateless.OrderBeforeAttribute<global::GameStateTest.AnotherModule>("process"),
            new global::Sparkitect.Stateless.OrderBeforeAttribute("update")
        ).BuildGraph(builder, context, global::GameStateTest.IDs.TransitionId.TestModule_init, /* SG statically analyzes Ownership, ownership requires type referencing an owning class (parent class or ParentIdAttribute) with IHasIdentification (i know that this on the right is invalid syntax)*/ ((IHasIdentification)OwningClass).Identification);

        // TestModule.Cleanup - OnDestroy
        // Ordering: runs before TestModule.Update
        new global::Sparkitect.GameState.OnDestroyScheduling(
            new global::Sparkitect.Stateless.OrderBeforeAttribute("update")
        ).BuildGraph(builder, context, global::GameStateTest.IDs.TransitionId.TestModule_cleanup);
    }
}
