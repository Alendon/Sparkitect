using System.Runtime.InteropServices;

namespace Sparkitect.ECS;

[StructLayout(LayoutKind.Explicit)]
public readonly struct EntityId
{
    [FieldOffset(0)] public readonly EntityIdKind Kind;

    [FieldOffset(4)] private readonly VolatileEntityId _volatile;

    [FieldOffset(4)] private readonly StableEntityId _stable;

    [FieldOffset(4)] private readonly ConstantEntityId _constant;
    
    private EntityId(VolatileEntityId id)
    {
        Kind = EntityIdKind.Volatile;
        _volatile = id;
    }
    
    private EntityId(StableEntityId id)
    {
        Kind = EntityIdKind.Stable;
        _stable = id;
    }
    
    private EntityId(ConstantEntityId id)
    {
        Kind = EntityIdKind.Constant;
        _constant = id;
    }

    public EntityId Invalid => default;

    public static EntityId CreateVolatile(uint archetype, uint index, uint version)
    {
        return new EntityId(new VolatileEntityId(archetype, index, version));
    }
    
    public static EntityId CreateStable(uint index, uint version)
    {
        return new EntityId(new StableEntityId(index, version));
    }
    
    public static EntityId CreateConstant(uint archetype, uint index, uint version)
    {
        return new EntityId(new ConstantEntityId(archetype, index, version));
    }

    public bool IsVolatile() => Kind == EntityIdKind.Volatile;

    public bool IsVolatile(out VolatileEntityId id)
    {
        if (Kind == EntityIdKind.Volatile)
        {
            id = _volatile;
            return true;
        }

        id = default;
        return false;
    }

    public bool IsStable() => Kind == EntityIdKind.Stable;

    public bool IsStable(out StableEntityId id)
    {
        if (Kind == EntityIdKind.Stable)
        {
            id = _stable;
            return true;
        }

        id = default;
        return false;
    }

    public bool IsConstant() => Kind == EntityIdKind.Constant;

    public bool IsConstant(out ConstantEntityId id)
    {
        if (Kind == EntityIdKind.Constant)
        {
            id = _constant;
            return true;
        }

        id = default;
        return false;
    }

    public enum EntityIdKind
    {
        Invalid,
        Volatile,
        Stable,
        Constant
    }
}

public readonly struct VolatileEntityId
{
    public readonly uint Archetype;
    public readonly uint Index;
    public readonly uint Version;

    internal VolatileEntityId(uint archetype, uint index, uint version)
    {
        Archetype = archetype;
        Index = index;
        Version = version;
    }
}

public readonly struct StableEntityId
{
    public readonly uint Index;
    public readonly uint Version;

    internal StableEntityId(uint index, uint version)
    {
        Index = index;
        Version = version;
    }
}

public readonly struct ConstantEntityId
{
    public readonly uint Archetype;
    public readonly uint Index;
    public readonly uint Version;

    internal ConstantEntityId(uint archetype, uint index, uint version)
    {
        Archetype = archetype;
        Index = index;
        Version = version;
    }
}