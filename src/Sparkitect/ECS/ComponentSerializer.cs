using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Sparkitect.Utils.Serialization;

namespace Sparkitect.ECS;

public abstract class ComponentSerializer
{
    public abstract void Serialize(IntPtr ptr, DataWriter writer, IWorld world, Entity entity);
    public abstract bool Deserialize(IntPtr ptr, DataReader reader, IWorld world, Entity entity);
}

[PublicAPI]
public abstract class ComponentSerializer<TComponent> : ComponentSerializer
    where TComponent : unmanaged, IComponent
{
    public abstract void Serialize(ref TComponent component, DataWriter writer, IWorld world, Entity entity);
    public abstract bool Deserialize(ref TComponent component, DataReader reader, IWorld world, Entity entity);
    
    public sealed override unsafe void Serialize(IntPtr ptr, DataWriter writer, IWorld world, Entity entity)
    {
        Serialize(ref Unsafe.AsRef<TComponent>((void*)ptr), writer, world, entity);
    }
    
    public sealed override unsafe bool Deserialize(IntPtr ptr, DataReader reader, IWorld world, Entity entity)
    {
        return Deserialize(ref Unsafe.AsRef<TComponent>((void*)ptr), reader, world, entity);
    }
}