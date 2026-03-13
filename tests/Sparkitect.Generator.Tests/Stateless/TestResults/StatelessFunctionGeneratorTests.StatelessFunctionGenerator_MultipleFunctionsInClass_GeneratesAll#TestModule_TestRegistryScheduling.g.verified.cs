//HintName: TestModule_TestRegistryScheduling.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace TestMod;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.ApplySchedulingEntrypointAttribute<global::Sparkitect.Stateless.TestFunctionAttribute>]
internal class TestModule_TestRegistryScheduling
    : global::Sparkitect.GameState.ApplySchedulingEntrypoint<global::Sparkitect.Stateless.TestFunctionAttribute, global::StatelessTest.TestContext, global::Sparkitect.Stateless.IExecutionGraphBuilder>
{
    public override void BuildGraph(
        global::Sparkitect.Stateless.IExecutionGraphBuilder builder,
        global::StatelessTest.TestContext context)
    {

        {


            global::Sparkitect.Stateless.OrderAfterAttribute[] param_0 = [

            ];



            global::Sparkitect.Stateless.OrderBeforeAttribute[] param_1 = [

            ];


            var scheduling = new global::StatelessTest.TestScheduling(

                param_0,

                param_1

            );
            scheduling.BuildGraph(builder, context, global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestModule.InitFunc>(), global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestModule>());
        }

        {


            global::Sparkitect.Stateless.OrderAfterAttribute[] param_0 = [

                new global::Sparkitect.Stateless.OrderAfterAttribute<global::TestMod.TestModule.InitFunc>()

            ];



            global::Sparkitect.Stateless.OrderBeforeAttribute[] param_1 = [

            ];


            var scheduling = new global::StatelessTest.TestScheduling(

                param_0,

                param_1

            );
            scheduling.BuildGraph(builder, context, global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestModule.UpdateFunc>(), global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestModule>());
        }

    }
}