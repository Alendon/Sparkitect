//HintName: DesktopState_desktop_init_Wrapper.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace GameStateTest;

public partial class DesktopState
{
    internal class desktop_initWrapper : global::Sparkitect.GameState.IStateMethod
    {

        private global::GameStateTest.ITestService _param0;


        [global::System.Diagnostics.DebuggerStepThroughAttribute]
        public void Execute()
        {
            Initialize(

    _param0  
);
        }

        public void Initialize(global::Sparkitect.DI.Container.ICoreContainer container, global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Type> facadeMap)
        {

            if(!container.TryResolveMapped<global::GameStateTest.ITestService>(out _param0, facadeMap))
            {
        
                throw new global::System.InvalidOperationException($"Failed to resolve dependency global::GameStateTest.ITestService for state function desktop_init");
        
            }

        }
    }
}