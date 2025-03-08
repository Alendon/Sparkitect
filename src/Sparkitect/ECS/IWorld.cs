namespace Sparkitect.ECS;

public interface IWorld
{
    public IEntityCollection Entities { get; }
    public ISystemCollection Systems { get; }
    
    
}