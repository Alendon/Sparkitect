using Sparkitect.Modding;

namespace Sparkitect.ECS;

public ref struct EntityModifier
{
    private IEntityManager _manager;
    private readonly ReadOnlySpan<Identification> _currentComponents;
    
    
}