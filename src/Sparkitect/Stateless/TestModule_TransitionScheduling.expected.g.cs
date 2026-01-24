//HintName: TestModule_TransitionScheduling.g.cs
// Expected SG output for TestModule transition function scheduling

using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Modding;
using Sparkitect.Stateless;

#pragma warning disable CS9113
#pragma warning disable CS1591

namespace GameStateTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.ApplySchedulingEntrypointAttribute<global::Sparkitect.Stateless.TransitionFunctionAttribute>]
internal class TestModule_TransitionScheduling
    : global::Sparkitect.GameState.ApplySchedulingEntrypoint<
        global::Sparkitect.Stateless.TransitionFunctionAttribute, TransitionContext>
{
    public override void BuildGraph(
        global::Sparkitect.Stateless.IExecutionGraphBuilder builder,
        global::Sparkitect.GameState.TransitionContext context)
    {
        {
            OrderAfterAttribute[] parm1 = [new OrderAfterAttribute<VulkanModule.VulkanInitFunc>()];
            OrderBeforeAttribute[] parm2 = [];
            var scheduling = new OnCreateScheduling(parm1, parm2);
            scheduling.BuildGraph(builder, context, TransitionFunctionID.Sparkitect.VulkanInit, VulkanModule.Identification);
        }
    }
}
