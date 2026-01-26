//HintName: TestModule_process_Wrapper.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

using TestMod.Generated.IdExtensions;

namespace TestMod;

public partial class TestModule
{
    public class ProcessFunc : global::Sparkitect.Stateless.IStatelessFunction, global::Sparkitect.Modding.IHasIdentification
    {
        public global::Sparkitect.Modding.Identification Identification => global::Sparkitect.Modding.IdentificationHelper.Read<ProcessFunc>();
        static global::Sparkitect.Modding.Identification global::Sparkitect.Modding.IHasIdentification.Identification => global::Sparkitect.Modding.IDs.TestID..Process;

        public global::Sparkitect.Modding.Identification ParentIdentification => global::Sparkitect.Modding.IdentificationHelper.Read<TestModule>();


        private global::TestMod.ILogger _param0;

        private global::TestMod.IConfig _param1;


        [global::System.Diagnostics.DebuggerStepThroughAttribute]
        public void Execute()
        {
            TestModule.Process(

                _param0,

                _param1

            );
        }

        public void Initialize(
            global::Sparkitect.DI.Container.ICoreContainer container,
            global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Type> facadeMap)
        {

            if (!container.TryResolveMapped<global::TestMod.ILogger>(out _param0, facadeMap))
            {

                throw new global::System.InvalidOperationException(
                    "Failed to resolve global::TestMod.ILogger for stateless function process");

            }

            if (!container.TryResolveMapped<global::TestMod.IConfig>(out _param1, facadeMap))
            {

                throw new global::System.InvalidOperationException(
                    "Failed to resolve global::TestMod.IConfig for stateless function process");

            }

        }
    }
}
