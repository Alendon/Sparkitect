//HintName: TestModule_init_Wrapper.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

using TestMod.Generated.IdExtensions;

namespace TestMod;

public partial class TestModule
{
    public class InitFunc : global::Sparkitect.Stateless.IStatelessFunction, global::Sparkitect.Modding.IHasIdentification
    {
        public global::Sparkitect.Modding.Identification Identification => global::Sparkitect.Modding.IdentificationHelper.Read<InitFunc>();
        static global::Sparkitect.Modding.Identification global::Sparkitect.Modding.IHasIdentification.Identification => global::Sparkitect.Modding.IDs.TestID.TestMod.Init;

        public global::Sparkitect.Modding.Identification ParentIdentification => global::Sparkitect.Modding.IdentificationHelper.Read<global::TestMod.TestModule>();



        [global::System.Diagnostics.DebuggerStepThroughAttribute]
        public void Execute()
        {
            TestModule.Initialize(

            );
        }

        public void Initialize(
            global::Sparkitect.DI.Container.ICoreContainer container,
            global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Type> facadeMap)
        {

        }
    }
}
