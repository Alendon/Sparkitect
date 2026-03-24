//HintName: TestModule_update_Wrapper.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

using TestMod.Generated.IdExtensions;

namespace TestMod;

public partial class TestModule
{
    public class UpdateFunc : global::Sparkitect.Stateless.IStatelessFunction, global::Sparkitect.Modding.IHasIdentification
    {
        public global::Sparkitect.Modding.Identification Identification => global::Sparkitect.Modding.IdentificationHelper.Read<UpdateFunc>();
        static global::Sparkitect.Modding.Identification global::Sparkitect.Modding.IHasIdentification.Identification => global::Sparkitect.Modding.IDs.TestID.TestMod.Update;



        [global::System.Diagnostics.DebuggerStepThroughAttribute]
        public void Execute()
        {
            TestModule.Update(

            );
        }

        public void Initialize(global::Sparkitect.DI.Resolution.IResolutionScope scope)
        {

        }
    }
}
