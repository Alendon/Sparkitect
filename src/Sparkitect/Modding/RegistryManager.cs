using DryIoc;
using OneOf;
using OneOf.Types;
using Sparkitect.DI;

namespace Sparkitect.Modding;

internal class RegistryManager : IRegistryManager
{
    private readonly Dictionary<ushort, Action<IContainer>> _registryBuilders = new();
    private readonly IModManager _modManager;
    private  readonly IIdentificationManager _identificationManager;

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
        
        using var categoryRegisterContainer =
            _modManager.CreateConfigurationContainer<IIoCRegistryBuilder>(true,
                OneOf<All, IEnumerable<string>>.FromT1(modsToInclude));

        var categoryRegistryContainers = categoryRegisterContainer.ResolveMany<IIoCRegistryBuilder>();

        var proxy = new RegistrationProxy(this);
        
        foreach (var categoryRegistryContainer in categoryRegistryContainers)
        {
            categoryRegistryContainer.ConfigureRegistries(proxy);
        }
    }

    private void RegisterObjects(RegistryPhase registryPhase)
    {
        //TODO implement registry phases
        //TODO sorting of registry category processing
        //TODO granular registry sorting (mod dependency, manual sorting (for overrides/extensions) )
        
        IEnumerable<string> modObjectsToRegister = _modManager.LoadedModsPerGroup[^1];
        
        using var objectRegisterContainer =
            _modManager.CreateConfigurationContainer<Registrations>(true,
                OneOf<All, IEnumerable<string>>.FromT1(modObjectsToRegister));

        foreach (var (_, builder) in _registryBuilders)
        {
            builder(objectRegisterContainer);
        }
        
        var objectRegistryContainers = objectRegisterContainer.ResolveMany<Registrations>();
        
        foreach (var objectRegistryContainer in objectRegistryContainers)
        {
            var registry = objectRegisterContainer.Resolve<IRegistry>(serviceKey: objectRegistryContainer.CategoryIdentifier);
            objectRegistryContainer.MainPhaseRegistration(registry);
        }
    }


    private class RegistrationProxy(RegistryManager parent) : IRegistryProxy
    {
        public void AddRegistry<TRegistry>(string categoryIdentifier) where TRegistry : IRegistry
        {
            var categoryId = parent._identificationManager.RegisterCategory(categoryIdentifier);
            parent._registryBuilders[categoryId] = container => container.Register<IRegistry, TRegistry>(Reuse.Singleton, serviceKey: categoryIdentifier);
        }
    }
}