//HintName: TestModule_TestRegistryScheduling.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace TestMod;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.Metadata.ApplyMetadataEntrypointAttribute<global::Sparkitect.Stateless.IScheduling>]
internal class TestModule_TestRegistryScheduling
    : global::Sparkitect.Metadata.ApplyMetadataEntrypoint<global::Sparkitect.Stateless.IScheduling>
{
    public override void CollectMetadata(
        global::System.Collections.Generic.Dictionary<global::Sparkitect.Modding.Identification, global::Sparkitect.Stateless.IScheduling> metadata)
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
            scheduling.OwnerId = global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestModule>();
            metadata[global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestModule.InitFunc>()] = scheduling;
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
            scheduling.OwnerId = global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestModule>();
            metadata[global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestModule.UpdateFunc>()] = scheduling;
        }

    }
}