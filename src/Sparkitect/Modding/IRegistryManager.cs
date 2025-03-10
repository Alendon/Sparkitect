namespace Sparkitect.Modding;

//manages the registry of categories (actual registries) and objects
public interface IRegistryManager
{
    void ProcessRegistry(
        RegistryPhase registryPhase = RegistryPhase.Category | RegistryPhase.ObjectPre | RegistryPhase.ObjectMain |
                                      RegistryPhase.ObjectPost);
    
    //TODO add unregistration method
}