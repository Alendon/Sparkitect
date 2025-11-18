namespace Sparkitect.Modding;

public interface IRegistryBase
{
    void Unregister(Identification id);
}

/// <summary>
/// Base interface for registry declarations
/// The implementing class must be partial and be annotated with the <see cref="RegistryAttribute"/>
/// </summary>
public interface IRegistry : IRegistryBase
{
    static abstract string Identifier { get; }
}

