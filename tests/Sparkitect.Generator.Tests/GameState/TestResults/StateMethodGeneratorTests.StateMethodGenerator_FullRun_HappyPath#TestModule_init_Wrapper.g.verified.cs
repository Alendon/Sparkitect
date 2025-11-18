//HintName: TestModule_init_Wrapper.g.cs
namespace GameStateTest;

public partial class TestModule
{
    internal class initWrapper : global::Sparkitect.GameState.IStateMethod
    {

        private global::GameStateTest.ITestService _param0;


        public void Execute()
        {
            Initialize(

                _param0  
            );
        }

        public void Initialize(global::Sparkitect.DI.Container.IFacadedCoreContainer container)
        {

    
            if(!container.TryResolve<global::GameStateTest.ITestService>(out _param0))
            {
        
                throw new global::System.InvalidOperationException($"Failed to resolve dependency global::GameStateTest.ITestService for state function init");
        
            }
    

        }
    }
}