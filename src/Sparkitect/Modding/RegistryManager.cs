using JetBrains.Annotations;
using OneOf;
using OneOf.Types;
using Serilog;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;

namespace Sparkitect.Modding;

[FacadeToRegistry<IRegistryManagerFacade>]
[Singleton<IRegistryManager>]
internal class RegistryManager : IRegistryManager, IRegistryManagerFacade
{
    internal required IModManager _modManager { get; init; }
    internal required IIdentificationManager _identificationManager { get; init; }
    internal required IGameStateManager _gameStateManager { get; init; }

    private int lastProcessedModGroup = -1;


    public void ProcessRegistry()
    {
        //TODO make sure, that the registry never goes out of sync with the mod manager
        //TODO currently there is no sorting between registries and/or mods. This is required long term

        var currentModGroup = _modManager.LoadedModsPerGroup.Count - 1;

        if (currentModGroup == lastProcessedModGroup)
        {
            Log.Warning("Called Process Registry without changes to the active mods");
            return;
        }

        if (Math.Abs(currentModGroup - lastProcessedModGroup) > 1)
        {
            throw new Exception();
        }

        var buildUp = currentModGroup > lastProcessedModGroup;
        var modsToProcess = _modManager.LoadedModsPerGroup[^1];

        if (buildUp)
        {
            ProcessRegistries(modsToProcess);
            ProcessRegistrations(modsToProcess);
        }
        else
        {
            ProcessDeregistrations(modsToProcess);
            RemoveRegistries(modsToProcess);
        }
    }
    

    private void ProcessRegistries(IEnumerable<string> modsToProcess)
    {
        using var factoryContainer = BuildRegistries(
            OneOf<All, IEnumerable<string>>.FromT1(modsToProcess));

        var all = factoryContainer.ResolveAll();
        
        foreach (var kvp in all)
        {
            var categoryKey = kvp.Key.Match(
                id => throw new InvalidOperationException("Registry key must be string, not Identification"),
                s => s);

            // Enforce globally unique category IDs
            if (_identificationManager.TryGetCategoryId(categoryKey, out _))
                throw new InvalidOperationException($"Category identifier '{categoryKey}' already registered");

            _identificationManager.RegisterCategory(categoryKey);
        }
    }
    
    private void RemoveRegistries(IReadOnlyList<string> modsToProcess)
    {
        throw new NotImplementedException();
    }

    private void ProcessRegistrations(IReadOnlyList<string> modsToProcess)
    {
        using var registrationsContainer =
            _modManager.CreateEntrypointContainer<Registrations>(OneOf<All, IEnumerable<string>>.FromT1(modsToProcess));
        var registrations = registrationsContainer.ResolveMany();
        
        using var registries = BuildRegistries(new All());

        // Initialize with container
        foreach (var reg in registrations)
        {
            reg.Initialize(_gameStateManager.CurrentCoreContainer);
        }

        foreach (var reg in registrations)
        {
            if (!registries.TryResolve(reg.CategoryIdentifier, out var registry))
                throw new InvalidOperationException($"Missing registry for category '{reg.CategoryIdentifier}'");
            reg.ProcessRegistrations(registry);
        }
    }
    
    private void ProcessDeregistrations(IReadOnlyList<string> modsToProcess)
    {
        using var registries = BuildRegistries(new All());

        foreach (var (keySet, registry) in registries.ResolveAll())
        {
            var categoryId = keySet.Match(_ => throw new InvalidOperationException(), x => x);

            foreach (var modId in modsToProcess)
            {
                /*
                 * TODO Instead of manually iterating over the ids from the identification manager,
                 * it would be more clean to extend the SGed Registrations classes to also handle unregistration
                 * By this we can also reset the associated object IDs while giving developers more option to customize
                 */
                
                
                var objectIds = _identificationManager.GetAllObjectIdsOfModAndCategory(modId, categoryId).ToArray();
                
                foreach (var objectId in objectIds)
                {
                    registry.Unregister(objectId);
                    _identificationManager.UnregisterObject(objectId);
                }
            }
        }
    }
    
    
    
    [MustDisposeResource]
    private IFactoryContainer<IRegistry> BuildRegistries(OneOf<All, IEnumerable<string>> mods)
    {
        using var configurators =
            _modManager.CreateEntrypointContainer<IRegistryConfigurator>(mods);

        var containerBuilder =
            new FactoryContainerBuilder<IRegistry>(_gameStateManager.CurrentCoreContainer, FactoryKeyType.String);

        configurators.ProcessMany(c => c.ConfigureRegistries(containerBuilder));

        return containerBuilder.Build();
    }
}