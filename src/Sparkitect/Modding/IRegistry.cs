namespace Sparkitect.Modding;

/// <summary>
/// Base interface for registry declarations
/// The implementing class must be partial and be annotated with the <see cref="RegistryAttribute"/>
/// </summary>
public interface IRegistry
{
    void Unregister(Identification id);
}