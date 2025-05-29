using OneOf;
using OneOf.Types;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Modding;

[Singleton<IRegistryManager>]
internal class RegistryManager : IRegistryManager
{
    private readonly Dictionary<ushort, Action<IFactoryContainerBuilder<Registrations>>> _registryBuilders = new();
    private readonly IModManager _modManager;
    private readonly IIdentificationManager _identificationManager;

    public RegistryManager(IModManager modManager, IIdentificationManager identificationManager)
    {
        _modManager = modManager;
        _identificationManager = identificationManager;
    }


    public void ProcessRegistry(
        RegistryPhase registryPhase = RegistryPhase.None | RegistryPhase.Category | RegistryPhase.ObjectPre |
                                      RegistryPhase.ObjectMain | RegistryPhase.ObjectPost)
    {
        //TODO make sure, that the registry never goes out of sync with the mod manager
        //As this must happen outside of the registry system, we cant utilize the event bus
        //Instead we probably need to add optional callbacks in the mod manager, to inform about state changes

        if (registryPhase == RegistryPhase.None) return;

        if ((registryPhase & RegistryPhase.Category) != 0)
        {
            RegisterCategories();
        }

        if ((registryPhase & (RegistryPhase.ObjectPre | RegistryPhase.ObjectMain | RegistryPhase.ObjectPost)) != 0)
        {
            RegisterObjects(registryPhase);
        }
    }

    private void RegisterCategories()
    {
        IEnumerable<string> modsToInclude = _modManager.LoadedModsPerGroup[^1];
        
    }

    private void RegisterObjects(RegistryPhase registryPhase)
    {
        
    }
    
}