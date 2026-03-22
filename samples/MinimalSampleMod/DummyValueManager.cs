using System.Diagnostics;
using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Commands;
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
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;

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
    private ICommandBufferAccessor? _commandBufferAccessor;

    public IWorld? GetWorld() => _world;
    


    public IWorld BuildWorld()
    {
        _world = IWorld.Create();
        _tracker = new ObjectTracker<IDisposable>();
        _frameTimer.Restart();
        _lastFrameTime = 0f;
        
        var compSize = componentManager.GetSize(UnmanagedComponentID.MinimalSampleMod.Minimal);


        var soaStorage = new SoAStorage([(UnmanagedComponentID.MinimalSampleMod.Minimal, compSize)], _tracker, _world, 32);
        var storageHandle = _world.AddStorage(soaStorage, soaStorage.CreateCapabilityRegistrations());
        
        _world.SetSystemTree(systemManager.BuildTree(EcsSystemGroupID.MinimalSampleMod.Minimal));

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
        
        var currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        var deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;
        systemManager.ExecuteSystems(_world, new FrameTiming(deltaTime, currentTime));

        // Retrieve accessor after ExecuteSystems (which triggers BuildWorldCache on first call)
        _commandBufferAccessor ??= systemManager.GetCommandBufferAccessor(_world);

        // Play back deferred structural mutations from command buffers
        if (_commandBufferAccessor is not null)
            _commandBufferAccessor.Playback();
    }

    public void DestroyWorld()
    {
        _commandBufferAccessor = null;
        _frameTimer.Stop();
        _world?.Dispose();
        _world = null;
    }

    
}