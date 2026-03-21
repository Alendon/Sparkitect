using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Components;
using Sparkitect.ECS.Storage;
using Sparkitect.ECS.Systems;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;

namespace MinimalSampleMod;

[StateService<IDummyValueManager, SampleModule>]
public class DummyValueManager(IComponentManager componentManager, ISystemManager systemManager) : IDummyValueManager, IDummyValueManagerStateFacade
{
    private readonly Dictionary<Identification, string> _values = [];
    
    public void AddDummyValue(Identification id, string value)
    {
        Log.Information("Registering value '{Value}' for '{Id}'", value, id);
        _values[id] = value;
    }

    public void RemoveDummyValue(Identification id)
    {
        _values.Remove(id);
    }

    public string GetDummyValue(Identification id)
    {
        return _values[id];
    }

    public string GetDummyFacaded(Identification id)
    {
        return _values[id];
    }

    private IWorld? _world;
    private IObjectTracker<IDisposable>? _tracker;

    public IWorld? GetWorld() => _world;
    


    public IWorld BuildWorld()
    {
        _world = IWorld.Create();
        _tracker = new ObjectTracker<IDisposable>();
        
        var compSize = componentManager.GetSize(UnmanagedComponentID.MinimalSampleMod.Minimal);


        var soaStorage = new SoAStorage([(UnmanagedComponentID.MinimalSampleMod.Minimal, compSize)], _tracker, _world, 32);
        var storageHandle = _world.AddStorage(soaStorage, soaStorage.CreateCapabilityRegistrations());
        _world.AddSystemGroup(EcsSystemGroupID.MinimalSampleMod.Minimal);
        _world.AddSystem(EcsSystemID.MinimalSampleMod.Sample);

        var storage = _world.GetStorage(storageHandle);
        for (int i = 0; i < 3; i++)
        {
            var entity = storage.AsStorage<int>()?.AllocateEntity();
            if (entity is null)
            {
                Log.Error("Entity allocation failed");
                return _world;
            }
            
            storage.As<IComponentAccess<int>>()!.Set(UnmanagedComponentID.MinimalSampleMod.Minimal, entity.Value,
                new MinimalComponent() { Value = Random.Shared.Next()});
        }
        
        
        systemManager.NotifyRebuild(_world);
        

        return _world;
    }
    
    public void SimulateWorld()
    {
        if (_world is null)
        {
            Log.Error("Simulate world while world is null");
            return;
        }
        
        systemManager.ExecuteSystems(_world);
        
    }

    public void DestroyWorld()
    {
        _world?.Dispose();
        _world = null;
        
    }

    
}