using Sparkitect.Modding;

namespace Sparkitect.ECS;

public interface IComponentManager
{
    public void AddComponent<TComponent, TComponentSerializer>(Identification id)
        where TComponent : unmanaged, IComponent
        where TComponentSerializer : ComponentSerializer<TComponent>;
    
    public void RemoveComponent(Identification id);
    
    public bool TryGetComponentSerializer(Identification id, out ComponentSerializer serializer);
    
    public int GetComponentSize(Identification id);
}