using Sparkitect.Modding;

namespace Sparkitect.ECS;

public interface IEntityCollection
{
    
    public EntityId CreateEntity(bool returnStableId = false);
    public EntityId CreateEntity(Identification archetypeId, bool returnStableId = false);
    public EntityId CreateEntity(Span<Identification> componentIds, bool returnStableId = false);
    
    // func: entity builder
    
    public bool EntityExists(EntityId entityId);
    
    public void AddComponents(EntityId entityId, Span<Identification> componentIds);
    public void RemoveComponents(EntityId entityId, Span<Identification> componentIds);
    public bool HasComponents(EntityId entityId, Span<Identification> componentIds);

    
    
    public void DestroyEntity(EntityId entityId);
    
    
    
    public Span<IEntityPool> GetPools();
    public IEntityPool GetPool(Identification archetypeId);
    public IEntityPool GetPool(Span<Identification> componentIds);
}