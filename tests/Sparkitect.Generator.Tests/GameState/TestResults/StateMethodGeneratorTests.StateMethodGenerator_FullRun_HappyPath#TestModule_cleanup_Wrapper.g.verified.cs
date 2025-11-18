//HintName: TestModule_cleanup_Wrapper.g.cs
namespace GameStateTest;

public partial class TestModule
{
    internal class cleanupWrapper : global::Sparkitect.GameState.IStateMethod
    {


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