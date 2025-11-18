//HintName: TestModule_update_Wrapper.g.cs
namespace GameStateTest;

public partial class TestModule
{
    internal class updateWrapper : global::Sparkitect.GameState.IStateMethod
    {

        private global::GameStateTest.ITestService _param0;

        private global::GameStateTest.ITestFacade _param1;


        public void Execute()
        {
            Update(

                _param0  ,
                _param1  
            );
        }

        public void Initialize(global::Sparkitect.DI.Container.IFacadedCoreContainer container)
        {

    
            if(!container.TryResolve<global::GameStateTest.ITestService>(out _param0))
            {
        
                throw new global::System.InvalidOperationException($"Failed to resolve dependency global::GameStateTest.ITestService for state function update");
        
            }
    

    
            if(!container.TryResolve<global::GameStateTest.ITestFacade>(out _param1))
            {
        
                throw new global::System.InvalidOperationException($"Failed to resolve dependency global::GameStateTest.ITestFacade for state function update");
        
            }
    

        }
    }
}