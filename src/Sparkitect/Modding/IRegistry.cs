namespace Sparkitect.Modding;

/// <summary>
/// Base interface for registry declarations
/// The implementing class must be partial and be annotated with the <see cref="RegistryAttribute"/>
/// </summary>
public interface IRegistry
{
    
    /// <summary>
    /// This method will be called before processing all registrations for this Registry
    /// </summary>
    void PreRegister();

    /// <summary>
    /// This method will be called after processing all registrations for this Registry
    /// </summary>
    void PostRegister();
    
 
    void Unregister(Identification id);
}