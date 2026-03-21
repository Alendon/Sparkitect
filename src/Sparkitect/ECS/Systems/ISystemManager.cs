using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

public interface ISystemManager
{
    void RegisterSystem(Identification id);
    void RegisterSystemGroup(Identification id);
    void ExecuteSystems(IWorld world);
    void NotifyRebuild(IWorld world);
    void NotifyDispose(IWorld world);
}
