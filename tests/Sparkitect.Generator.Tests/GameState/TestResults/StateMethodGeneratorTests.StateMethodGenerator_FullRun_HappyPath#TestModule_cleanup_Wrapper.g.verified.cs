//HintName: TestModule_cleanup_Wrapper.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace GameStateTest;

public partial class TestModule
{
    internal class cleanupWrapper : global::Sparkitect.GameState.IStateMethod
    {


        [global::System.Diagnostics.DebuggerStepThroughAttribute]
        public void Execute()
        {
            Cleanup(

);
        }

        public void Initialize(global::Sparkitect.DI.Container.ICoreContainer container, global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Type> facadeMap)
        {

        }
    }
}