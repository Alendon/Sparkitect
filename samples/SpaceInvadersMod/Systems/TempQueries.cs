using Sparkitect.ECS;
using Sparkitect.ECS.Queries;
using Sparkitect.Modding;

namespace SpaceInvadersMod;

class BulletQuery : ComponentQuery<EntityId>
{
    protected internal BulletQuery(IWorld world, IReadOnlyList<Identification> componentIds, List<StorageHandle> matchedStorages, FilterHandle filterHandle) : base(world, componentIds, matchedStorages, filterHandle)
    {
    }
}
    
class EnemyQuery : ComponentQuery<EntityId>
{
    protected internal EnemyQuery(IWorld world, IReadOnlyList<Identification> componentIds, List<StorageHandle> matchedStorages, FilterHandle filterHandle) : base(world, componentIds, matchedStorages, filterHandle)
    {
    }
}
    
class PlayerQuery : ComponentQuery<EntityId>
{
    protected internal PlayerQuery(IWorld world, IReadOnlyList<Identification> componentIds, List<StorageHandle> matchedStorages, FilterHandle filterHandle) : base(world, componentIds, matchedStorages, filterHandle)
    {
    }
}