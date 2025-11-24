using Sparkitect.Modding;

namespace Sparkitect.ECS;

public interface IComponentBase
{
    bool Dirty { get; set; }
}

public interface IComponent : IComponentBase
{
    //TODO How to attach a custom serializer to a component?
    //each component should define a serializer which instance is used to serialize/deserialize the component data
}