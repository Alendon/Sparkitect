using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

public partial class CoreModule
{
    internal class LoadRootModsWrapper : IStateMethod
    {
        private IModManager _modManager;
        
        public void Execute()
        {
            LoadRootMods(_modManager);
        }

        public void Initialize(IFacadedCoreContainer container)
        {
            container.Resolve<IModManager>();
        }
    }

    internal class ProcessStateRegistryWrapper : IStateMethod
    {
        private IRegistryManagerFacade _registryManagerFacade;
        
        public void Execute()
        {
            ProcessStateRegistry(_registryManagerFacade);
        }

        public void Initialize(IFacadedCoreContainer container)
        {
            _registryManagerFacade = container.ResolveFacaded<IRegistryManagerFacade>();
        }
    }
}